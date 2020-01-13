using System;

namespace Efcore31.Auditable
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class AuditedAttribute : Attribute
    {
        public string TableName { get; set; }

        public AuditedAttribute() { }
    }
}
