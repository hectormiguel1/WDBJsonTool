namespace WDBJsonTool.XIII.Extraction;

/// <summary>
/// Entry point for XIII WDB extraction.
/// Now uses the XIIIWDBParser which inherits from WDBParserBase.
/// </summary>
internal static class ExtractionMain
{
    public static void StartExtraction(string inWDBfile, bool shouldIgnoreKnown)
    {
        var parser = new XIIIWDBParser(shouldIgnoreKnown);
        parser.Parse(inWDBfile);
    }
}