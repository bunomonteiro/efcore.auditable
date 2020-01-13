using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Newtonsoft.Json;

namespace Efcore31.Auditable
{
    public class AuditEntry
    {
        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
            Action = entry.State.ToString();
        }

        public EntityEntry Entry { get; }
        public string TableName { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> KeyValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();
        public List<PropertyEntry> TemporaryProperties { get; } = new List<PropertyEntry>();

        public bool HasTemporaryProperties => TemporaryProperties.Any();

        public static implicit operator Audit(AuditEntry entry)
        {
            if(entry == null)
            {
                return null;
            }

            Audit audit = new Audit
            {
                TableName = entry.TableName,
                Action = entry.Action,
                DateTime = DateTime.UtcNow,
                KeyValues = JsonConvert.SerializeObject(entry.KeyValues),
                OldValues = entry.OldValues.Count == 0 ? null : JsonConvert.SerializeObject(entry.OldValues),
                NewValues = entry.NewValues.Count == 0 ? null : JsonConvert.SerializeObject(entry.NewValues)
            };

            return audit;
        }
    }
}
