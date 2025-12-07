namespace WDBJsonTool.XIII2LR.Extraction;

/// <summary>
/// Entry point for XIII-2/LR WDB extraction.
/// Now uses the XIII2LRWDBParser which inherits from WDBParserBase.
/// </summary>
internal static class ExtractionMain
{
    public static void StartExtraction(string inWDBfile)
    {
        var parser = new XIII2LRWDBParser();
        parser.Parse(inWDBfile);
    }
}