using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AccessUtility.Engine;
using AccessUtility.Exporters;
using AccessUtility.Models;
using Xunit;

namespace AccessUtility.Tests
{
    public class Jet3MemoryReaderTests : IDisposable
    {
        private readonly string _testDbPath;

        public Jet3MemoryReaderTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_mem_{Guid.NewGuid():N}.mdb");
            TestRunner.CreateSampleDatabase(_testDbPath);
        }

        public void Dispose()
        {
            if (File.Exists(_testDbPath))
            {
                try { File.Delete(_testDbPath); } catch { }
            }
        }

        [Fact]
        public void Jet3MemoryReader_LoadsDatabase_AndReadsHeaderPage()
        {
            using var reader = new Jet3MemoryReader(_testDbPath);

            Assert.True(reader.IsValid);
            Assert.True(reader.TotalPages >= 2);
            Assert.True(reader.FileSizeBytes >= Jet3MemoryReader.PageSize);

            var page0 = reader.GetPage(0);
            Assert.Equal(Jet3MemoryReader.PageSize, page0.Length);

            string magic = Encoding.ASCII.GetString(page0.Slice(4, 15)).TrimEnd('\0');
            Assert.Contains("Jet DB", magic);
        }

        [Fact]
        public void Jet3MemoryReader_OutOfBoundsPage_ReturnsEmptySpan()
        {
            using var reader = new Jet3MemoryReader(_testDbPath);

            var negativePage = reader.GetPage(-1);
            Assert.True(negativePage.IsEmpty);

            var outOfBounds = reader.GetPage(99999);
            Assert.True(outOfBounds.IsEmpty);
        }

        [Fact]
        public void Jet3MemoryReader_ReadPageCopy_ReturnsIdenticalBytes()
        {
            using var reader = new Jet3MemoryReader(_testDbPath);

            var span = reader.GetPage(0);
            var copy = reader.ReadPageCopy(0);

            Assert.Equal(span.Length, copy.Length);
            Assert.Equal(span.ToArray(), copy);
        }

        [Fact]
        public void Jet3MemoryReader_StreamTableRows_StreamsRecordsCorrectly()
        {
            var db = Jet3BinaryReader.ReadDatabase(_testDbPath, out _);
            Assert.NotEmpty(db.Tables);

            var table = db.Tables[0];

            using var reader = new Jet3MemoryReader(_testDbPath);
            var streamedRows = new List<AccessRow>(reader.StreamTableRows(table));

            Assert.Equal(table.Rows.Count, streamedRows.Count);
        }

        [Fact]
        public async Task Jet3MemoryReader_StreamTableRowsAsync_WithCancellation_Works()
        {
            var db = Jet3BinaryReader.ReadDatabase(_testDbPath, out _);
            var table = db.Tables[0];

            using var reader = new Jet3MemoryReader(_testDbPath);
            var streamedRows = new List<AccessRow>();

            await foreach (var row in reader.StreamTableRowsAsync(table))
            {
                streamedRows.Add(row);
            }

            Assert.Equal(table.Rows.Count, streamedRows.Count);

            // Test cancellation token
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var cancelledRows = new List<AccessRow>();
            await foreach (var row in reader.StreamTableRowsAsync(table, cts.Token))
            {
                cancelledRows.Add(row);
            }

            Assert.Empty(cancelledRows);
        }

        [Fact]
        public async Task CsvExporter_ExportTableStreamAsync_ProducesValidCsv()
        {
            var db = Jet3BinaryReader.ReadDatabase(_testDbPath, out _);
            var table = db.Tables[0];

            using var reader = new Jet3MemoryReader(_testDbPath);
            string csvOutput = Path.Combine(Path.GetTempPath(), $"stream_out_{Guid.NewGuid():N}.csv");

            try
            {
                var exportedPath = await CsvExporter.ExportTableStreamAsync(table, reader.StreamTableRowsAsync(table), csvOutput);
                Assert.True(File.Exists(exportedPath));

                var lines = File.ReadAllLines(exportedPath);
                Assert.True(lines.Length >= 1); // Header + Rows
            }
            finally
            {
                if (File.Exists(csvOutput)) File.Delete(csvOutput);
            }
        }
    }
}
