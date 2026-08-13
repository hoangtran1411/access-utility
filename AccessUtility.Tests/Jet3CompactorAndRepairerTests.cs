using System;
using System.IO;
using AccessUtility.Engine;
using Xunit;

namespace AccessUtility.Tests
{
    public class Jet3CompactorAndRepairerTests
    {
        [Fact]
        public void Compact_ValidDatabase_CreatesCompactedFileAndPreservesSchema()
        {
            string srcMdb = Path.Combine(Path.GetTempPath(), $"src_{Guid.NewGuid():N}.mdb");
            string targetMdb = Path.Combine(Path.GetTempPath(), $"target_{Guid.NewGuid():N}.mdb");

            try
            {
                // Initialize synthetic database
                AccessUtility.Tests.TestRunner.CreateSampleDatabase(srcMdb);
                Assert.True(File.Exists(srcMdb));

                var res = Jet3Compactor.Compact(srcMdb, targetMdb, forceUnlock: true);
                Assert.True(res.Success);
                Assert.True(File.Exists(targetMdb));
                Assert.True(res.CompactedSizeBytes > 0);
            }
            finally
            {
                if (File.Exists(srcMdb)) File.Delete(srcMdb);
                if (File.Exists(targetMdb)) File.Delete(targetMdb);
            }
        }

        [Fact]
        public void Repair_ValidOrSlightlyCorruptDatabase_ReconstructsDatabase()
        {
            string srcMdb = Path.Combine(Path.GetTempPath(), $"corrupt_{Guid.NewGuid():N}.mdb");
            string repairedMdb = Path.Combine(Path.GetTempPath(), $"repaired_{Guid.NewGuid():N}.mdb");

            try
            {
                AccessUtility.Tests.TestRunner.CreateSampleDatabase(srcMdb);
                Assert.True(File.Exists(srcMdb));

                var res = Jet3Repairer.Repair(srcMdb, repairedMdb, forceUnlock: true);
                Assert.True(res.Success);
                Assert.True(File.Exists(repairedMdb));
                Assert.True(res.TotalPagesScanned > 0);
            }
            finally
            {
                if (File.Exists(srcMdb)) File.Delete(srcMdb);
                if (File.Exists(repairedMdb)) File.Delete(repairedMdb);
            }
        }
    }
}
