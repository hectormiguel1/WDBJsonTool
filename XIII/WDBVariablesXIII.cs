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
    public List<uint> StrtypelistValues = [];
    public List<uint> TypelistValues = [];
    public bool IsKnown;
    public bool IgnoreKnown;
    public string? SheetName;

    // Section names
    public const string SheetNameSectionName = "!!sheetname";
    public const string StringSectionName = "!!string";
    public const string StrtypelistSectionName = "!!strtypelist";
    public const string TypelistSectionName = "!!typelist";
    public const string VersionSectionName = "!!version";
    public const string StructItemSectionName = "!structitem";

    // Section names string length
    public const int StringSectionNameLength = 8;
    public const int StrtypelistSectionNameLength = 13;
    public const int TypelistSectionNameLength = 10;
    public const int VersionSectionNameLength = 9;

    // Section data
    public byte[]? StringsData;
    public byte[]? StrtypelistData;
    public byte[]? TypelistData;
    public byte[]? VersionData;
}
