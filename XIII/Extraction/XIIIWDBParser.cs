using System.Text.Json;
using WDBJsonTool.Common;
using WDBJsonTool.Common.Parsers;

namespace WDBJsonTool.XIII.Extraction;

/// <summary>
/// WDB parser implementation for Final Fantasy XIII.
/// </summary>
internal class XIIIWDBParser(bool ignoreKnown = false) : WDBParserBase<WDBVariablesXIII>
{
    protected override void InitializeVariables(string wdbFilePath, WDBVariablesXIII variables)
    {
        base.InitializeVariables(wdbFilePath, variables);

        // XIII-specific initialization
        variables.WDBName = Path.GetFileNameWithoutExtension(wdbFilePath);
        variables.IgnoreKnown = ignoreKnown;
    }

    protected override void ParseSections(BinaryReader reader, WDBVariablesXIII variables)
    {
        SectionsParser.MainSections(reader, variables);
    }

    protected override void WriteSectionsToJson(WDBVariablesXIII variables, Utf8JsonWriter jsonWriter)
    {
        SectionsParser.MainSectionsToJson(variables, jsonWriter);
    }

    protected override void ParseRecords(BinaryReader reader, WDBVariablesXIII variables, Utf8JsonWriter jsonWriter)
    {
        if (variables.IsKnown)
        {
            RecordsParser.ParseRecordsWithFields(reader, variables, jsonWriter);
        }
        else
        {
            RecordsParser.ParseRecordsWithoutFields(reader, variables, jsonWriter);
        }
    }

    // Public method for external use
    public void Parse(string wdbFilePath)
    {
        var variables = new WDBVariablesXIII();
        Extract(wdbFilePath, variables);
    }
}