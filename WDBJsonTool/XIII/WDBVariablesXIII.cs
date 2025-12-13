namespace WDBJsonTool.XIII
{
    internal class WDBVariablesXIII
    {
        // Important variables
        public string? WDBName;
        public string? WDBFilePath;
        public string? JsonFilePath;
        public uint RecordCount;
        public bool HasStringSection;
        public List<uint> StrtypelistValues = [];
        public List<uint> TypelistValues = [];
        public bool IsKnown;
        public bool IgnoreKnown;
        public string? SheetName;
        public string[]? Fields;
        public uint RecordCountWithSections;
        public Dictionary<string, List<object>> RecordsDataDict = new();
        public readonly Dictionary<string, uint> ProcessedStringsDict = new();
        public readonly Dictionary<string, byte[]> OutPerRecordData = new();

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
        public uint FieldCount;
    }
}