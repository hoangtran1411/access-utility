using System.Collections.Generic;
using System.Text.Json.Serialization;
using AccessUtility.Engine;
using AccessUtility.Models;

namespace AccessUtility.Web
{
    [JsonSerializable(typeof(DiagnosticReport))]
    [JsonSerializable(typeof(LockFileInfo))]
    [JsonSerializable(typeof(LdbLockEntry))]
    [JsonSerializable(typeof(CleanLockResponse))]
    [JsonSerializable(typeof(ExportResponse))]
    [JsonSerializable(typeof(CompactResult))]
    [JsonSerializable(typeof(RepairResult))]
    [JsonSerializable(typeof(AccessDatabase))]
    [JsonSerializable(typeof(AccessTable))]
    [JsonSerializable(typeof(AccessColumn))]
    [JsonSerializable(typeof(List<AccessTable>))]
    [JsonSerializable(typeof(List<AccessColumn>))]
    [JsonSerializable(typeof(List<Dictionary<string, object?>>))]
    [JsonSerializable(typeof(Dictionary<string, object?>))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(LogEntry), TypeInfoPropertyName = "LogEntry")]
    [JsonSerializable(typeof(List<LogEntry>), TypeInfoPropertyName = "ListLogEntry")]
    public partial class AppJsonContext : JsonSerializerContext
    {
    }
}
