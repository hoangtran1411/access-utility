using System.IO;
using System.Text;
using AccessUtility.Engine;
using Xunit;

namespace AccessUtility.Tests
{
    public class LdbLockInspectorTests
    {
        [Fact]
        public void Inspect_WhenNoLdbExists_ReturnsFalseAndEmptyUsers()
        {
            string tempMdb = Path.Combine(Path.GetTempPath(), $"test_{System.Guid.NewGuid():N}.mdb");
            File.WriteAllBytes(tempMdb, new byte[2048]);

            try
            {
                var lockInfo = LdbLockInspector.Inspect(tempMdb);
                Assert.False(lockInfo.Exists);
                Assert.Empty(lockInfo.ConnectedUsers);
            }
            finally
            {
                if (File.Exists(tempMdb)) File.Delete(tempMdb);
            }
        }

        [Fact]
        public void Inspect_WhenLdbExists_ParsesComputerAndUserNames()
        {
            string tempMdb = Path.Combine(Path.GetTempPath(), $"test_{System.Guid.NewGuid():N}.mdb");
            string tempLdb = Path.ChangeExtension(tempMdb, ".ldb");

            File.WriteAllBytes(tempMdb, new byte[2048]);

            byte[] lockData = new byte[64];
            byte[] comp = Encoding.ASCII.GetBytes("DESKTOP-TEST");
            byte[] user = Encoding.ASCII.GetBytes("TestUser");
            Array.Copy(comp, 0, lockData, 0, comp.Length);
            Array.Copy(user, 0, lockData, 32, user.Length);

            File.WriteAllBytes(tempLdb, lockData);

            try
            {
                var lockInfo = LdbLockInspector.Inspect(tempMdb);
                Assert.True(lockInfo.Exists);
                Assert.Single(lockInfo.ConnectedUsers);
                Assert.Equal("DESKTOP-TEST", lockInfo.ConnectedUsers[0].ComputerName);
                Assert.Equal("TestUser", lockInfo.ConnectedUsers[0].UserName);
                Assert.True(lockInfo.IsOrphanLock);
            }
            finally
            {
                if (File.Exists(tempMdb)) File.Delete(tempMdb);
                if (File.Exists(tempLdb)) File.Delete(tempLdb);
            }
        }

        [Fact]
        public void TryCleanOrphanLock_WhenNotLocked_RemovesLdbFile()
        {
            string tempMdb = Path.Combine(Path.GetTempPath(), $"test_{System.Guid.NewGuid():N}.mdb");
            string tempLdb = Path.ChangeExtension(tempMdb, ".ldb");

            File.WriteAllBytes(tempMdb, new byte[2048]);
            File.WriteAllBytes(tempLdb, new byte[64]);

            try
            {
                bool cleaned = LdbLockInspector.TryCleanOrphanLock(tempMdb, out string msg);
                Assert.True(cleaned);
                Assert.False(File.Exists(tempLdb));
            }
            finally
            {
                if (File.Exists(tempMdb)) File.Delete(tempMdb);
                if (File.Exists(tempLdb)) File.Delete(tempLdb);
            }
        }
    }
}
