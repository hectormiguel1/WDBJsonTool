using WDBJsonTool.Common;

namespace WDBJsonTool.XIII;

/// <summary>
/// Variables container for Final Fantasy XIII WDB format.
/// Extends base with XIII-specific properties like known file detection and WDBDicts support.
/// </summary>
internal class WDBVariablesXIII : WDBVariablesBase
{
    // XIII-specific variables
    public string? WDBName;
    public bool HasStringSection;
    public List<uint> StrtypelistValues = new();
    public List<uint> TypelistValues = new();
    public bool IsKnown;
    public bool IgnoreKnown;
    public string? SheetName;

    // Section names
    public readonly string SheetNameSectionName = "!!sheetname";
    public readonly string StringSectionName = "!!string";
    public readonly string StrtypelistSectionName = "!!strtypelist";
    public readonly string TypelistSectionName = "!!typelist";
    public readonly string VersionSectionName = "!!version";
    public readonly string StructItemSectionName = "!structitem";

    // Section names string length
    public readonly int StringSectionNameLength = 8;
    public readonly int StrtypelistSectionNameLength = 13;
    public readonly int TypelistSectionNameLength = 10;
    public readonly int VersionSectionNameLength = 9;

    // Section data
    public byte[]? StringsData;
    public byte[]? StrtypelistData;
    public byte[]? TypelistData;
    public byte[]? VersionData;
    public uint FieldCount;
}
