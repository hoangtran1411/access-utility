using System;
using System.Collections.Generic;
using System.IO;
using AccessUtility.Engine;
using AccessUtility.Models;
using Xunit;

namespace AccessUtility.Tests
{
    public class OleExtractorTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _outputDir;

        public OleExtractorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"AccessUtility_OleTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            _outputDir = Path.Combine(_tempDir, "extracted");
        }

        private byte[] CreateMockOleData(byte[] fileSignature, int oleHeaderSize = 78)
        {
            byte[] oleData = new byte[oleHeaderSize + fileSignature.Length + 10]; // header + sig + some payload
            
            // Fill "Access OLE Header" with dummy bytes
            for (int i = 0; i < oleHeaderSize; i++)
            {
                oleData[i] = (byte)(i % 255);
            }

            // Write the actual file signature
            Array.Copy(fileSignature, 0, oleData, oleHeaderSize, fileSignature.Length);

            return oleData;
        }

        [Theory]
        [InlineData(new byte[] { 0x42, 0x4D }, "bmp")]
        [InlineData(new byte[] { 0xFF, 0xD8, 0xFF }, "jpg")]
        [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, "png")]
        [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46 }, "pdf")]
        [InlineData(new byte[] { 0xD0, 0xCF, 0x11, 0xE0 }, "doc")]
        public void ExtractDatabase_SupportedSignatures_ExtractsCorrectFileType(byte[] signature, string expectedExtension)
        {
            // Arrange
            var db = new AccessDatabase { FilePath = "Test.mdb" };
            var table = new AccessTable { Name = "Images" };
            table.Columns.Add(new AccessColumn { Name = "Picture", DataType = JetDataType.Binary });
            
            var row = new Dictionary<string, object?>();
            row["Picture"] = CreateMockOleData(signature);
            table.Rows.Add(row);
            
            db.Tables.Add(table);

            // Act
            var report = OleExtractor.ExtractDatabase(db, _outputDir);

            // Assert
            Assert.Single(report.ExtractedFiles);
            var file = report.ExtractedFiles[0];
            Assert.Equal("Images", file.TableName);
            Assert.Equal("Picture", file.ColumnName);
            Assert.Equal(0, file.RowIndex);
            Assert.Equal(expectedExtension, file.FileType);
            Assert.True(File.Exists(file.FilePath));
        }

        [Fact]
        public void ExtractDatabase_NoOleColumns_DoesNothing()
        {
            // Arrange
            var db = new AccessDatabase { FilePath = "Test.mdb" };
            var table = new AccessTable { Name = "Data" };
            table.Columns.Add(new AccessColumn { Name = "ID", DataType = JetDataType.Integer });
            
            var row = new Dictionary<string, object?>();
            row["ID"] = 1;
            table.Rows.Add(row);
            
            db.Tables.Add(table);

            // Act
            var report = OleExtractor.ExtractDatabase(db, _outputDir);

            // Assert
            Assert.Empty(report.ExtractedFiles);
            Assert.False(Directory.Exists(_outputDir)); // Shouldn't be created if no tables to process
        }

        [Fact]
        public void ExtractDatabase_UnknownSignature_DoesNotExtract()
        {
            // Arrange
            var db = new AccessDatabase { FilePath = "Test.mdb" };
            var table = new AccessTable { Name = "Data" };
            table.Columns.Add(new AccessColumn { Name = "Blob", DataType = JetDataType.Binary });
            
            var row = new Dictionary<string, object?>();
            // completely unknown signature
            row["Blob"] = CreateMockOleData(new byte[] { 0x00, 0x01, 0x02, 0x03 });
            table.Rows.Add(row);
            
            db.Tables.Add(table);

            // Act
            var report = OleExtractor.ExtractDatabase(db, _outputDir);

            // Assert
            Assert.Empty(report.ExtractedFiles);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, true); } catch { }
        }
    }
}
