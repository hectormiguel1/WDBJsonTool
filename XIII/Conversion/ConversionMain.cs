namespace WDBJsonTool.XIII.Conversion;

/// <summary>
/// Entry point for XIII WDB conversion (JSON to WDB).
/// Now uses the XIIIWDBConverter which inherits from WDBConverterBase.
/// </summary>
internal static class ConversionMain
{
    public static void StartConversion(string jsonFilePath)
    {
        var converter = new XIIIWDBConverter();
        converter.ConvertToWDB(jsonFilePath);
    }
}