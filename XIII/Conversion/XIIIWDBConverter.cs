using WDBJsonTool.Common.Converters;

namespace WDBJsonTool.XIII.Conversion;

/// <summary>
/// WDB converter implementation for Final Fantasy XIII (JSON to WDB).
/// </summary>
internal class XIIIWDBConverter : WDBConverterBase<WDBVariablesXIII>
{
    protected override void DeserializeJson(string jsonFilePath, WDBVariablesXIII variables)
    {
        JsonDeserializer.DeserializeData(jsonFilePath, variables);
    }

    protected override void ConvertRecords(WDBVariablesXIII variables)
    {
        if (variables.Fields?.Length > 0)
        {
            RecordsConversion.ConvertRecordsWithFields(variables);
        }
        else
        {
            RecordsConversion.ConvertRecordsNoFields(variables);
        }
    }

    protected override void WriteSections(BinaryWriter writer, WDBVariablesXIII variables)
    {
        // Sections are written as part of BuildWDB
    }

    protected override void WriteRecords(BinaryWriter writer, WDBVariablesXIII variables)
    {
        // Records are written as part of BuildWDB
    }

    protected override void BuildWDBFile(WDBVariablesXIII variables)
    {
        // XIII uses its own BuildWDB method
        WDBbuilder.BuildWDB(variables);
    }

    // Public method for external use
    public void ConvertToWDB(string jsonFilePath)
    {
        var variables = new WDBVariablesXIII();
        Convert(jsonFilePath, variables);
    }
}