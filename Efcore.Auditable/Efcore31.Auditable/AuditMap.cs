using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Efcore31.Auditable
{
    public class AuditMap : IEntityTypeConfiguration<Audit>
    {
        internal const string DefaultTableName = "Audits";
        internal string TableName { get; private set; }

        public AuditMap(string tableName = DefaultTableName)
        {
            TableName = GetAuditTableName(tableName, DefaultTableName);
        }

        public void Configure(EntityTypeBuilder<Audit> builder)
        {
            builder.ToTable(TableName);

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            builder.Property(x => x.TableName).IsRequired();
            builder.Property(x => x.Action).IsRequired();
            builder.Property(x => x.DateTime).IsRequired();
            builder.Property(x => x.KeyValues).IsRequired(false);
            builder.Property(x => x.NewValues).IsRequired(false);
            builder.Property(x => x.OldValues).IsRequired(false);
        }

        internal static string GetAuditTableName(string tableName, string suffix = "")
        {
            if(string.IsNullOrWhiteSpace(tableName))
            {
                return DefaultTableName;
            } else
            {
                string nameLower = tableName.ToLower();
                string suffixLower = suffix.ToLower();

                if(nameLower != suffixLower && !nameLower.EndsWith(suffixLower))
                {
                    return tableName + suffix;
                }

                return tableName;
            }
        }
    }
}
