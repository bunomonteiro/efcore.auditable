using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Efcore31.Auditable
{
    public static class AuditableExtensions
    {
        public static void InitializeAudit<T>(this T context, Func<DbContextOptionsBuilder, DbContextOptionsBuilder> builder) where T : DbContext
        {
            InitializeAuditedTablesMetadataCache(context);

            using(AuditableContext auditContext = new AuditableContext(builder(new DbContextOptionsBuilder()).Options))
            {
                IRelationalDatabaseCreator creator = auditContext.GetService<IRelationalDatabaseCreator>();
                creator.CreateTables();
            }
        }

        private static void InitializeAuditedTablesMetadataCache<T>(T context) where T : DbContext
        {
            Type contextType = context.GetType();
            if(!AuditableContext.AuditedTablesMetadataCache.ContainsKey(contextType))
            {
                AuditableContext.AuditedTablesMetadataCache.TryAdd(contextType, context.GetAllAuditedTablesMetadata());
            }
        }

        internal static List<(Type type, string name)> GetAllAuditedTablesMetadata<T>(this T context) where T : DbContext
        {
            List<PropertyInfo> tables = context.GetType()
            .GetProperties()
            .Where(p =>
                p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)
            ).ToList();

            List<(Type type, string name)> auditedTables = tables.Select(t =>
            {
                Type type = null;
                string tableName = string.Empty;
                object[] attrs = t.GetCustomAttributes(typeof(AuditedAttribute), true);

                if(attrs.Length > 0)
                {
                    type = t.PropertyType.IsGenericType ? t.PropertyType.GetGenericArguments().First() : t.PropertyType;
                    tableName = getTableName((AuditedAttribute)attrs[0], type);
                } else if((attrs = t.PropertyType.GetGenericArguments().First().GetCustomAttributes(typeof(AuditedAttribute), true)).Length > 0)
                {
                    type = t.PropertyType.GetGenericArguments().First();
                    tableName = getTableName((AuditedAttribute)attrs[0], type);
                } else
                {
                    return (null, null);
                }

                return (type, tableName);

                string getTableName(AuditedAttribute attr, Type type)
                {
                    return AuditMap.GetAuditTableName(string.IsNullOrWhiteSpace(attr.TableName) ? type.Name : attr.TableName);
                }
            }).Where(t => t.type != null).ToList();

            return auditedTables;
        }
    }
}
