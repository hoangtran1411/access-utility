using System;
using System.Collections.Generic;

namespace AccessUtility.Models
{
    public class CarvedRecord
    {
        public string TableName { get; set; } = string.Empty;
        public int PageIndex { get; set; }
        public int ByteOffset { get; set; }
        public bool IsDeletedSlot { get; set; }
        public double ConfidenceScore { get; set; }
        public string ConfidenceRating { get; set; } = "High"; // High, Medium, Low
        public Dictionary<string, object?> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public object? this[string columnName]
        {
            get => Values.TryGetValue(columnName, out var val) ? val : null;
            set => Values[columnName] = value;
        }
    }

    public class CarvedTableSummary
    {
        public string TableName { get; set; } = string.Empty;
        public int ActiveRowsCount { get; set; }
        public int DeletedRowsSalvaged { get; set; }
        public double AverageConfidence { get; set; }
    }

    public class ForensicCarveReport
    {
        public string DatabasePath { get; set; } = string.Empty;
        public int TotalPagesScanned { get; set; }
        public int ActivePagesCount { get; set; }
        public int SlackPagesScanned { get; set; }
        public int ActiveRowsCount { get; set; }
        public int SalvagedDeletedRowsCount { get; set; }
        public int HighConfidenceCount { get; set; }
        public int MediumConfidenceCount { get; set; }
        public int LowConfidenceCount { get; set; }
        public List<CarvedTableSummary> TableSummaries { get; set; } = new();
        public List<CarvedRecord> CarvedRecords { get; set; } = new();
        public string? ExportedPath { get; set; }
        public string SummaryMessage { get; set; } = string.Empty;
    }
}
