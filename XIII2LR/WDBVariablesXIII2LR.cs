using WDBJsonTool.Common;

namespace WDBJsonTool.XIII2LR;
/// <summary>
/// Variables container for Final Fantasy XIII-2 and Lightning Returns WDB format.
/// Extends base with XIII2LR-specific properties like string arrays and dynamic field definitions.
/// </summary>
internal class WDBVariablesXIII2LR : WDBVariablesBase
{
    // XIII2LR-specific variables
    public bool HasStrArraySection;
    public bool HasStringSection;
    public bool ParseStrtypelistAsV1;
    public bool HasTypelistSection;
    public readonly List<uint> StrArrayOffsets = [];
    public readonly List<string> NumStringFields = [];
    public List<string> ProcessStringsList = [];
    public readonly Dictionary<string, List<string>> StrArrayDict = new();
    public List<int> StrtypelistValues = [];

    // Section names
    public const string SheetNameSectionName = "!!sheetname";
    public const string StrArraySectionName = "!!strArray";
    public const string StrArrayInfoSectionName = "!!strArrayInfo";
    public const string StrArrayListSectionName = "!!strArrayList";
    public const string StringSectionName = "!!string";
    public const string StrtypelistSectionName = "!!strtypelist";
    public const string StrtypelistbSectionName = "!!strtypelistb";
    public const string TypelistSectionName = "!!typelist";
    public const string VersionSectionName = "!!version";
    public const string StructItemSectionName = "!structitem";
    public const string StructItemNumSectionName = "!structitemnum";

    // Section names string length
    public const int SheetNameSectionNameLength = 11;
    public const int StrArraySectionNameLength = 10;
    public const int StrArrayInfoSectionNameLength = 14;
    public const int StrArrayListSectionNameLength = 14;
    public const int StringSectionNameLength = 8;
    public const int StrtypelistSectionNameLength = 13;
    public const int StrtypelistbSectionNameLength = 14;
    public const int TypelistSectionNameLength = 10;
    public const int VersionSectionNameLength = 9;
    public const int StructItemSectionNameLength = 11;
    public const int StructItemNumSectionNameLength = 14;

    // Section data
    public string? SheetName;
    public byte[]? SheetNameData;
    public byte[]? StrArrayData;
    public byte[]? StrArrayInfoData;
    public byte OffsetsPerValue;
    public byte BitsPerOffset;
    public byte[]? StrArrayListData;
    public byte[]? StringsData;
    public byte[]? StrtypelistData;
    public byte[]? TypelistData;
    public byte[]? VersionData;
    public byte[]? StructItemData;
    public byte[]? StructItemNumData;
}
