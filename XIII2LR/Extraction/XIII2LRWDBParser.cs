using System.Text.Json;
using WDBJsonTool.Common;
using WDBJsonTool.Common.Parsers;

namespace WDBJsonTool.XIII2LR.Extraction;

/// <summary>
/// WDB parser implementation for Final Fantasy XIII-2 and Lightning Returns.
/// </summary>
internal class XIII2LRWDBParser : WDBParserBase<WDBVariablesXIII2LR>
{
    protected override void ParseSections(BinaryReader reader, WDBVariablesXIII2LR variables)
    {
        SectionsParser.MainSections(reader, variables);
    }

    protected override void WriteSectionsToJson(WDBVariablesXIII2LR variables, Utf8JsonWriter jsonWriter)
    {
        SectionsParser.MainSectionsToJson(variables, jsonWriter);
    }

    protected override void ParseRecords(BinaryReader reader, WDBVariablesXIII2LR variables, Utf8JsonWriter jsonWriter)
    {
        RecordsParser.ProcessRecords(reader, variables, jsonWriter);
    }

    // Public method for external use
    public void Parse(string wdbFilePath)
    {
        var variables = new WDBVariablesXIII2LR();
        Extract(wdbFilePath, variables);
    }
}