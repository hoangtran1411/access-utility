using AccessUtility.Engine;
using Xunit;

namespace AccessUtility.Tests
{
    public class AxAssistantTests
    {
        [Fact]
        public void InterpretQuery_ExtractsActionsAndFilePath()
        {
            string query = "Compact my database sample97.mdb and clean stale locks";
            var plan = AxAssistant.InterpretQuery(query);

            Assert.Equal("sample97.mdb", plan.TargetFile);
            Assert.Contains("compact", plan.ActionSteps);
            Assert.Contains("clean-lock", plan.ActionSteps);
        }

        [Fact]
        public void InterpretQuery_ExtractsExportFormat()
        {
            string query = "Convert sample97.mdb to csv format";
            var plan = AxAssistant.InterpretQuery(query);

            Assert.Equal("sample97.mdb", plan.TargetFile);
            Assert.Contains("export", plan.ActionSteps);
            Assert.Equal("csv", plan.ExportFormat);
        }
    }
}
