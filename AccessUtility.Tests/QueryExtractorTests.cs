using System;
using System.Collections.Generic;
using System.IO;
using AccessUtility.Engine;
using AccessUtility.Models;
using Xunit;

namespace AccessUtility.Tests
{
    public class QueryExtractorTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _outputDir;

        public QueryExtractorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"AccessUtility_QueryTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            _outputDir = Path.Combine(_tempDir, "extracted");
        }

        private AccessDatabase CreateMockDbWithQueries()
        {
            var db = new AccessDatabase { FilePath = "Test.mdb" };
            
            // MSysObjects
            var msysObjects = new AccessTable { Name = "MSysObjects" };
            msysObjects.Rows.Add(new Dictionary<string, object?> { { "Id", 100 }, { "Name", "ActiveUsers" }, { "Type", 5 } });
            msysObjects.Rows.Add(new Dictionary<string, object?> { { "Id", 101 }, { "Name", "~sq_cTemp" }, { "Type", 5 } }); // hidden
            
            // MSysQueries
            var msysQueries = new AccessTable { Name = "MSysQueries" };
            
            // Query 100: ActiveUsers
            // SELECT Name, Age FROM Users WHERE Age > 18 ORDER BY Name DESC
            msysQueries.Rows.Add(new Dictionary<string, object?> { { "ObjectId", 100 }, { "Attribute", 1 }, { "Expression", "Name" }, { "Order", 1 } });
            msysQueries.Rows.Add(new Dictionary<string, object?> { { "ObjectId", 100 }, { "Attribute", 1 }, { "Name1", "Age" }, { "Order", 2 } });
            msysQueries.Rows.Add(new Dictionary<string, object?> { { "ObjectId", 100 }, { "Attribute", 2 }, { "Name1", "Users" }, { "Order", 3 } });
            msysQueries.Rows.Add(new Dictionary<string, object?> { { "ObjectId", 100 }, { "Attribute", 3 }, { "Expression", "Age > 18" }, { "Order", 4 } });
            msysQueries.Rows.Add(new Dictionary<string, object?> { { "ObjectId", 100 }, { "Attribute", 6 }, { "Expression", "Name" }, { "Name2", "D" }, { "Order", 5 } }); // Desc

            // Query 101: System Query
            msysQueries.Rows.Add(new Dictionary<string, object?> { { "ObjectId", 101 }, { "Attribute", 1 }, { "Expression", "1" }, { "Order", 1 } });
            
            db.Tables.Add(msysObjects);
            db.Tables.Add(msysQueries);
            
            return db;
        }

        [Fact]
        public void ExtractQueries_ValidQueries_GeneratesSql()
        {
            var db = CreateMockDbWithQueries();
            var report = QueryExtractor.ExtractQueries(db, _outputDir);

            // Assert
            Assert.Single(report.Queries); // Because 101 is hidden (starts with ~)
            var q = report.Queries[0];
            Assert.Equal("ActiveUsers", q.Name);
            Assert.Equal(100, q.ObjectId);
            
            Assert.Contains("SELECT Name, Age", q.SqlText);
            Assert.Contains("FROM Users", q.SqlText);
            Assert.Contains("WHERE Age > 18", q.SqlText);
            Assert.Contains("ORDER BY Name DESC", q.SqlText);
            
            string sqlFilePath = Path.Combine(_outputDir, "ActiveUsers.sql");
            Assert.True(File.Exists(sqlFilePath));
            
            string fileContent = File.ReadAllText(sqlFilePath);
            Assert.Contains("-- Query: ActiveUsers", fileContent);
        }

        [Fact]
        public void ExtractQueries_MissingSystemTables_ReturnsEmptyReport()
        {
            var db = new AccessDatabase { FilePath = "Empty.mdb" };
            var report = QueryExtractor.ExtractQueries(db, _outputDir);
            
            Assert.Empty(report.Queries);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
