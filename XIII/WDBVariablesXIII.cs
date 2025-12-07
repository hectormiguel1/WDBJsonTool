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
        public List<uint> StrtypelistValues = new();
        public List<uint> TypelistValues = new();
        public bool IsKnown;
        public bool IgnoreKnown;
        public string? SheetName;
        public string[]? Fields;
        public uint RecordCountWithSections;
        public Dictionary<string, List<object>> RecordsDataDict = new();
        public Dictionary<string, uint> ProcessedStringsDict = new();
        public Dictionary<string, byte[]> OutPerRecordData = new();

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
}