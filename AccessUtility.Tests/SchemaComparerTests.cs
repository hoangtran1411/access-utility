using System;
using System.Collections.Generic;
using System.IO;
using AccessUtility.Engine;
using AccessUtility.Exporters;
using AccessUtility.Models;
using Xunit;

namespace AccessUtility.Tests
{
    public class SchemaComparerTests : IDisposable
    {
        private readonly string _tempDir;

        public SchemaComparerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"AccessUtility_SchemaTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        private static AccessDatabase CreateMockDb(string name)
        {
            return new AccessDatabase
            {
                FilePath = $"C:\\{name}.mdb",
                Tables = new List<AccessTable>()
            };
        }

        [Fact]
        public void Compare_IdenticalSchemas_ReturnsNoDifferences()
        {
            var source = CreateMockDb("Src");
            var target = CreateMockDb("Tgt");

            var tableSrc = new AccessTable { Name = "Users", RecordCount = 10 };
            tableSrc.Columns.Add(new AccessColumn { Name = "ID", DataType = JetDataType.Integer });
            tableSrc.Rows.Add(new Dictionary<string, object?> { { "ID", 1 } });
            
            var tableTgt = new AccessTable { Name = "Users", RecordCount = 10 };
            tableTgt.Columns.Add(new AccessColumn { Name = "ID", DataType = JetDataType.Integer });
            tableTgt.Rows.Add(new Dictionary<string, object?> { { "ID", 1 } });

            source.Tables.Add(tableSrc);
            target.Tables.Add(tableTgt);

            var diff = SchemaComparer.Compare(source, target);

            Assert.False(diff.HasDifferences);
            Assert.Empty(diff.AddedTables);
            Assert.Empty(diff.RemovedTables);
            Assert.Empty(diff.ModifiedTables);
        }

        [Fact]
        public void Compare_TableAdded_DetectsAddedTable()
        {
            var source = CreateMockDb("Src");
            var target = CreateMockDb("Tgt");

            var tableSrc = new AccessTable { Name = "NewTable" };
            source.Tables.Add(tableSrc);

            var diff = SchemaComparer.Compare(source, target);

            Assert.True(diff.HasDifferences);
            Assert.Single(diff.AddedTables);
            Assert.Equal("NewTable", diff.AddedTables[0].TableName);
        }

        [Fact]
        public void Compare_TableRemoved_DetectsRemovedTable()
        {
            var source = CreateMockDb("Src");
            var target = CreateMockDb("Tgt");

            var tableTgt = new AccessTable { Name = "OldTable" };
            target.Tables.Add(tableTgt);

            var diff = SchemaComparer.Compare(source, target);

            Assert.True(diff.HasDifferences);
            Assert.Single(diff.RemovedTables);
            Assert.Equal("OldTable", diff.RemovedTables[0].TableName);
        }

        [Fact]
        public void Compare_ColumnAddedAndRemoved_DetectsColumnChanges()
        {
            var source = CreateMockDb("Src");
            var target = CreateMockDb("Tgt");

            var tableSrc = new AccessTable { Name = "Data" };
            tableSrc.Columns.Add(new AccessColumn { Name = "KeepCol", DataType = JetDataType.Text });
            tableSrc.Columns.Add(new AccessColumn { Name = "NewCol", DataType = JetDataType.Integer });
            
            var tableTgt = new AccessTable { Name = "Data" };
            tableTgt.Columns.Add(new AccessColumn { Name = "KeepCol", DataType = JetDataType.Text });
            tableTgt.Columns.Add(new AccessColumn { Name = "OldCol", DataType = JetDataType.DateTime });

            source.Tables.Add(tableSrc);
            target.Tables.Add(tableTgt);

            var diff = SchemaComparer.Compare(source, target);

            Assert.True(diff.HasDifferences);
            Assert.Single(diff.ModifiedTables);
            
            var modTable = diff.ModifiedTables[0];
            Assert.Single(modTable.AddedColumns);
            Assert.Equal("NewCol", modTable.AddedColumns[0].ColumnName);
            
            Assert.Single(modTable.RemovedColumns);
            Assert.Equal("OldCol", modTable.RemovedColumns[0].ColumnName);
        }

        [Fact]
        public void Compare_ColumnTypeChanged_DetectsModification()
        {
            var source = CreateMockDb("Src");
            var target = CreateMockDb("Tgt");

            var tableSrc = new AccessTable { Name = "Data" };
            tableSrc.Columns.Add(new AccessColumn { Name = "ID", DataType = JetDataType.LongInteger }); // changed
            
            var tableTgt = new AccessTable { Name = "Data" };
            tableTgt.Columns.Add(new AccessColumn { Name = "ID", DataType = JetDataType.Integer }); // old

            source.Tables.Add(tableSrc);
            target.Tables.Add(tableTgt);

            var diff = SchemaComparer.Compare(source, target);

            Assert.True(diff.HasDifferences);
            Assert.Single(diff.ModifiedTables);
            
            var modTable = diff.ModifiedTables[0];
            Assert.Single(modTable.ModifiedColumns);
            Assert.Equal("ID", modTable.ModifiedColumns[0].ColumnName);
            Assert.Equal(JetDataType.Integer, modTable.ModifiedColumns[0].OldDataType);
            Assert.Equal(JetDataType.LongInteger, modTable.ModifiedColumns[0].NewDataType);
            Assert.True(modTable.ModifiedColumns[0].TypeChanged);
        }

        [Fact]
        public void GenerateMigrationScript_CreatesSqliteScript()
        {
            var diff = new SchemaDiffResult { SourcePath = "Src", TargetPath = "Tgt", HasDifferences = true };
            
            var tableDiff = new TableDiff { TableName = "Items" };
            tableDiff.AddedColumns.Add(new ColumnDiff { ColumnName = "Price", NewDataType = JetDataType.Currency });
            tableDiff.RemovedColumns.Add(new ColumnDiff { ColumnName = "OldPrice" });
            
            diff.ModifiedTables.Add(tableDiff);

            string outPath = Path.Combine(_tempDir, "mig.sql");
            MigrationScriptExporter.GenerateMigrationScript(diff, outPath, "sqlite");

            Assert.True(File.Exists(outPath));
            string script = File.ReadAllText(outPath);
            Assert.Contains("ALTER TABLE [Items] ADD COLUMN [Price]", script);
            Assert.Contains("SQLite does not support DROP COLUMN", script);
        }
        
        [Fact]
        public void GenerateMigrationScript_CreatesPgsqlScript()
        {
            var diff = new SchemaDiffResult { SourcePath = "Src", TargetPath = "Tgt", HasDifferences = true };
            
            var tableDiff = new TableDiff { TableName = "Items" };
            tableDiff.AddedColumns.Add(new ColumnDiff { ColumnName = "Price", NewDataType = JetDataType.Currency });
            tableDiff.RemovedColumns.Add(new ColumnDiff { ColumnName = "OldPrice" });
            
            diff.ModifiedTables.Add(tableDiff);

            string outPath = Path.Combine(_tempDir, "mig_pg.sql");
            MigrationScriptExporter.GenerateMigrationScript(diff, outPath, "pgsql");

            Assert.True(File.Exists(outPath));
            string script = File.ReadAllText(outPath);
            Assert.Contains("ALTER TABLE \"Items\" ADD COLUMN \"Price\"", script);
            Assert.Contains("ALTER TABLE \"Items\" DROP COLUMN \"OldPrice\"", script);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
