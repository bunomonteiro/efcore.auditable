﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Efcore31.Auditable
{
    /// <summary>
    /// Class based on Gérald Barré example
    /// <para />
    /// <see cref="https://www.meziantou.net/entity-framework-core-history-audit-table.htm"/>
    /// </summary>
    public class AuditableContext : DbContext
    {
        internal static ConcurrentDictionary<Type, List<(Type type, string name)>> AuditedTablesMetadataCache = new ConcurrentDictionary<Type, List<(Type type, string name)>>();

        public DbSet<Audit> GeneralAudits { get; set; }

        public AuditableContext() : base() { }
        public AuditableContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new AuditMap("GeneralAudits"));

            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            List<AuditEntry> auditEntries = OnBeforeSaveChanges();
            int result = base.SaveChanges();
            OnAfterSaveChanges(auditEntries);
            return result;
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            List<AuditEntry> auditEntries = OnBeforeSaveChanges();
            int result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            await OnAfterSaveChanges(auditEntries);
            return result;
        }

        private List<AuditEntry> OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            List<AuditEntry> auditEntries = new List<AuditEntry>();

            foreach(EntityEntry entry in ChangeTracker.Entries())
            {
                if(!AuditedTablesMetadataCache.ContainsKey(GetType()) ||
                    !AuditedTablesMetadataCache[GetType()].Any(t => t.type == entry.Entity.GetType()) ||
                    entry.Entity is Audit ||
                    entry.State == EntityState.Detached ||
                    entry.State == EntityState.Unchanged
                )
                {
                    continue;
                }

                AuditEntry auditEntry = new AuditEntry(entry)
                {
                    TableName = entry.Metadata.GetTableName()
                };
                auditEntries.Add(auditEntry);

                foreach(PropertyEntry property in entry.Properties)
                {
                    if(property.IsTemporary)
                    {
                        // value will be generated by the database, get the value after saving
                        auditEntry.TemporaryProperties.Add(property);
                        continue;
                    }

                    string propertyName = property.Metadata.Name;
                    if(property.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[propertyName] = property.CurrentValue;
                        continue;
                    }

                    switch(entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;

                        case EntityState.Modified:
                            if(property.IsModified)
                            {
                                auditEntry.OldValues[propertyName] = property.OriginalValue;
                                auditEntry.NewValues[propertyName] = property.CurrentValue;
                            }
                            break;
                    }
                }
            }

            // Save audit entities that have all the modifications
            foreach(AuditEntry auditEntry in auditEntries.Where(_ => !_.HasTemporaryProperties))
            {
                Set<Audit>().Add(auditEntry);
            }

            // keep a list of entries where the value of some properties are unknown at this step
            return auditEntries.Where(_ => _.HasTemporaryProperties).ToList();
        }

        private Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
        {
            if(auditEntries == null || auditEntries.Count == 0)
            {
                return Task.CompletedTask;
            }

            foreach(AuditEntry auditEntry in auditEntries)
            {
                // Get the final value of the temporary properties
                foreach(PropertyEntry prop in auditEntry.TemporaryProperties)
                {
                    if(prop.Metadata.IsPrimaryKey())
                    {
                        auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                    } else
                    {
                        auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                }

                // Save the Audit entry
                Set<Audit>().Add(auditEntry);
            }

            return SaveChangesAsync();
        }
    }
}
