using WDBJsonTool.Common.Converters;

namespace WDBJsonTool.XIII2LR.Conversion;

/// <summary>
/// WDB converter implementation for Final Fantasy XIII-2/LR (JSON to WDB).
/// </summary>
internal class XIII2LRWDBConverter : WDBConverterBase<WDBVariablesXIII2LR>
{
    protected override void DeserializeJson(string jsonFilePath, WDBVariablesXIII2LR variables)
    {
        JsonDeserializer.DeserializeData(jsonFilePath, variables);
    }

    protected override void ConvertRecords(WDBVariablesXIII2LR variables)
    {
        RecordsConversion.ConvertRecordsStrArray(variables);
    }

    protected override void WriteSections(BinaryWriter writer, WDBVariablesXIII2LR variables)
    {
        // Sections are written as part of BuildWDB
    }

    protected override void WriteRecords(BinaryWriter writer, WDBVariablesXIII2LR variables)
    {
        // Records are written as part of BuildWDB
    }

    protected override void BuildWDBFile(WDBVariablesXIII2LR variables)
    {
        // XIII2LR uses its own BuildWDB method
        WDBbuilder.BuildWDB(variables);
    }

    // Public method for external use
    public void ConvertToWDB(string jsonFilePath)
    {
        var variables = new WDBVariablesXIII2LR();
        Convert(jsonFilePath, variables);
    }
}
