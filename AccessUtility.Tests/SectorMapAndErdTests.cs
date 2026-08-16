using System;
using System.Collections.Generic;
using System.IO;
using AccessUtility.Engine;
using AccessUtility.Models;
using Xunit;

namespace AccessUtility.Tests
{
    public class SectorMapAndErdTests
    {
        private AccessDatabase CreateTestDbWithRelationships()
        {
            var db = new AccessDatabase { FilePath = "test_rel.mdb" };

            var customers = new AccessTable
            {
                Name = "Customers",
                TdefPage = 2,
                Columns = new List<AccessColumn>
                {
                    new AccessColumn { Name = "CustomerID", DataType = JetDataType.Autonumber, IsAutoNumber = true },
                    new AccessColumn { Name = "CompanyName", DataType = JetDataType.Text },
                    new AccessColumn { Name = "City", DataType = JetDataType.Text }
                }
            };
            customers.Rows.Add(new Dictionary<string, object?>
            {
                ["CustomerID"] = 1,
                ["CompanyName"] = "Acme Corp",
                ["City"] = "New York"
            });

            var orders = new AccessTable
            {
                Name = "Orders",
                TdefPage = 3,
                Columns = new List<AccessColumn>
                {
                    new AccessColumn { Name = "OrderID", DataType = JetDataType.Autonumber, IsAutoNumber = true },
                    new AccessColumn { Name = "CustomerID", DataType = JetDataType.LongInteger },
                    new AccessColumn { Name = "OrderDate", DataType = JetDataType.DateTime },
                    new AccessColumn { Name = "Freight", DataType = JetDataType.Currency }
                }
            };
            orders.Rows.Add(new Dictionary<string, object?>
            {
                ["OrderID"] = 1001,
                ["CustomerID"] = 1,
                ["OrderDate"] = DateTime.UtcNow,
                ["Freight"] = 15.50m
            });

            var orderDetails = new AccessTable
            {
                Name = "OrderDetails",
                TdefPage = 4,
                Columns = new List<AccessColumn>
                {
                    new AccessColumn { Name = "OrderDetailID", DataType = JetDataType.Autonumber, IsAutoNumber = true },
                    new AccessColumn { Name = "OrderID", DataType = JetDataType.LongInteger },
                    new AccessColumn { Name = "UnitPrice", DataType = JetDataType.Currency },
                    new AccessColumn { Name = "Quantity", DataType = JetDataType.Integer }
                }
            };
            orderDetails.Rows.Add(new Dictionary<string, object?>
            {
                ["OrderDetailID"] = 5001,
                ["OrderID"] = 1001,
                ["UnitPrice"] = 99.00m,
                ["Quantity"] = 2
            });

            db.Tables.Add(customers);
            db.Tables.Add(orders);
            db.Tables.Add(orderDetails);

            return db;
        }

        [Fact]
        public void ErdGenerator_Generates_Valid_Mermaid_And_Markdown()
        {
            var db = CreateTestDbWithRelationships();
            var erd = ErdGenerator.GenerateErd(db);

            Assert.Equal(3, erd.TableCount);
            Assert.True(erd.RelationshipCount >= 2);
            Assert.Contains("erDiagram", erd.MermaidCode);
            Assert.Contains("Customers ||--o{ Orders", erd.MermaidCode);
            Assert.Contains("Orders ||--o{ OrderDetails", erd.MermaidCode);
            Assert.Contains("Customers {", erd.MermaidCode);
            Assert.Contains("int CustomerID PK", erd.MermaidCode);
            Assert.Contains("int CustomerID FK", erd.MermaidCode);

            // Test markdown export
            string mdPath = Path.Combine(Path.GetTempPath(), $"erd_test_{Guid.NewGuid():N}.md");
            try
            {
                ErdGenerator.ExportErdToMarkdown(db, mdPath);
                Assert.True(File.Exists(mdPath));
                string mdContent = File.ReadAllText(mdPath);
                Assert.Contains("# Entity Relationship Diagram", mdContent);
                Assert.Contains("```mermaid", mdContent);
                Assert.Contains("Customers", mdContent);
            }
            finally
            {
                if (File.Exists(mdPath)) File.Delete(mdPath);
            }
        }

        [Fact]
        public void SectorMapAnalyzer_Classifies_Pages_On_Sample_Database()
        {
            string samplePath = Path.Combine(AppContext.BaseDirectory, "sample97.mdb");
            if (!File.Exists(samplePath))
            {
                // Fallback to workspace root
                samplePath = Path.Combine(Directory.GetCurrentDirectory(), "sample97.mdb");
            }

            if (File.Exists(samplePath))
            {
                var report = SectorMapAnalyzer.AnalyzeSectorMap(samplePath);
                Assert.True(report.TotalPages > 0);
                Assert.True(report.HeaderPages >= 1);
                Assert.True(report.Pages.Count == report.TotalPages);

                var headerPage = report.Pages[0];
                Assert.Equal("Header", headerPage.PageType);
                Assert.Equal("Valid", headerPage.Status);

                var hexView = SectorMapAnalyzer.GetPageHexView(samplePath, 0);
                Assert.Equal(0, hexView.PageIndex);
                Assert.Equal(128, hexView.HexLines.Count); // 2048 / 16 = 128 rows
                Assert.Equal("000000", hexView.HexLines[0].Offset);
                Assert.NotEmpty(hexView.RawBase64);
            }
        }

        [Fact]
        public void SectorMapAnalyzer_Handles_Synthesized_Database_Pages()
        {
            string tempDb = Path.Combine(Path.GetTempPath(), $"synth_{Guid.NewGuid():N}.mdb");
            try
            {
                // Synthesize 4 pages (Header, PAM, TDEF, Data)
                byte[] data = new byte[2048 * 4];
                
                // Page 0: Header
                data[0] = 0x00;
                data[1] = 0x01;
                var magic = System.Text.Encoding.ASCII.GetBytes("Standard Jet DB");
                Array.Copy(magic, 0, data, 4, magic.Length);

                // Page 1: PAM
                data[2048] = 0x01;
                data[2049] = 0x01;

                // Page 2: TDEF
                data[2048 * 2] = 0x02;
                data[2048 * 2 + 1] = 0x01;

                // Page 3: Data
                data[2048 * 3] = 0x01;
                data[2048 * 3 + 1] = 0x01;
                BitConverter.GetBytes((uint)2).CopyTo(data, 2048 * 3 + 4); // TDEF ptr = 2
                BitConverter.GetBytes((ushort)10).CopyTo(data, 2048 * 3 + 8); // 10 records

                File.WriteAllBytes(tempDb, data);

                var report = SectorMapAnalyzer.AnalyzeSectorMap(tempDb);
                Assert.Equal(4, report.TotalPages);
                Assert.Equal("Header", report.Pages[0].PageType);
                Assert.Equal("PAM", report.Pages[1].PageType);
                Assert.Equal("TDEF", report.Pages[2].PageType);
                Assert.Equal("Data", report.Pages[3].PageType);
                Assert.Equal((uint)2, report.Pages[3].TdefPage);
                Assert.Equal(10, report.Pages[3].RecordCount);

                var hex = SectorMapAnalyzer.GetPageHexView(tempDb, 3);
                Assert.Equal(3, hex.PageIndex);
                Assert.Equal(128, hex.HexLines.Count);
                Assert.Contains("Data", hex.PageType);
            }
            finally
            {
                if (File.Exists(tempDb)) File.Delete(tempDb);
            }
        }

        [Fact]
        public void AxAssistant_Interprets_Erd_Query()
        {
            var plan = AxAssistant.InterpretQuery("Generate mermaid ERD diagram for inventory.mdb");
            Assert.Contains("erd", plan.ActionSteps);
        }
    }
}
