using System;
using AccessUtility.Models;

namespace AccessUtility.Exporters
{
    public static class SqlScriptExporter
    {
        public static string ExportDatabase(AccessDatabase db, string outputSqlPath, SqlDialect dialect = SqlDialect.Ansi)
        {
            var options = new SqlMigrationOptions
            {
                Dialect = dialect,
                IncludeForeignKeys = true,
                IncludeViews = true,
                UseTransactions = true
            };
            return SqlMigrationExporter.ExportDatabase(db, outputSqlPath, options);
        }
    }
}
