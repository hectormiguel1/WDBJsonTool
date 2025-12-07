using System.Collections.Generic;

namespace WDBJsonTool.Common;
public abstract class WDBVariablesBase
{
    public string? WDBFilePath { get; set; }
    public string? JsonFilePath { get; set; }
    public uint RecordCount { get; set; }
    public string[]? Fields { get; set; }
    public uint RecordCountWithSections { get; set; }
    public Dictionary<string, List<object>> RecordsDataDict { get; set; } = new();
    public Dictionary<string, uint> ProcessedStringsDict { get; set; } = new();
    public Dictionary<string, byte[]> OutPerRecordData { get; set; } = new();
    public uint FieldCount { get; set; }
}
