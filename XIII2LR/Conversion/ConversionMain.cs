namespace WDBJsonTool.XIII2LR.Conversion;

/// <summary>
/// Entry point for XIII-2/LR WDB conversion (JSON to WDB).
/// Now uses the XIII2LRWDBConverter which inherits from WDBConverterBase.
/// </summary>
internal static class ConversionMain
{
    public static void StartConversion(string jsonFilePath)
    {
        var converter = new XIII2LRWDBConverter();
        converter.ConvertToWDB(jsonFilePath);
    }
}
