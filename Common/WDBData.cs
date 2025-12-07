namespace WDBJsonTool.Common;

public record WDBData(
    string? SheetName,
    uint RecordCount,
    string[] Fields,
    Dictionary<string, List<object>> Records
);
