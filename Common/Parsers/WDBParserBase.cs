using System.Text.Json;
using WDBJsonTool.Extensions;
using WDBJsonTool.Support;

namespace WDBJsonTool.Common.Parsers;

/// <summary>
/// Abstract base class for WDB file parsers.
/// Implements the template method pattern for common extraction flow.
/// </summary>
public abstract class WDBParserBase<TVariables> where TVariables : WDBVariablesBase
{
    protected const string WPD_MAGIC = "WPD";
    protected const int MAGIC_LENGTH = 3;

    /// <summary>
    /// Main extraction method that orchestrates the parsing process.
    /// This is the template method that defines the algorithm skeleton.
    /// </summary>
    /// <param name="wdbFilePath">Path to the WDB file to parse</param>
    /// <param name="variables">Variables object to store parsed data</param>
    public void Extract(string wdbFilePath, TVariables variables)
    {
        using var wdbReader = new BinaryReader(File.Open(wdbFilePath, FileMode.Open, FileAccess.Read, FileShare.Read));

        InitializeVariables(wdbFilePath, variables);
        ValidateHeader(wdbReader);
        ReadRecordCount(wdbReader, variables);
        ParseSections(wdbReader, variables);

        Log.Info($"Total records: {variables.RecordCount}");

        WriteToJson(wdbReader, variables);
    }

    /// <summary>
    /// Initializes variables with file-specific information.
    /// </summary>
    protected virtual void InitializeVariables(string wdbFilePath, TVariables variables)
    {
        var fileName = Path.GetFileNameWithoutExtension(wdbFilePath);
        var directory = Path.GetDirectoryName(wdbFilePath);
        variables.WDBFilePath = wdbFilePath;
        variables.JsonFilePath = Path.Combine(directory ?? ".", fileName + ".json");
    }

    /// <summary>
    /// Validates the WDB file header (magic bytes).
    /// </summary>
    protected virtual void ValidateHeader(BinaryReader reader)
    {
        reader.BaseStream.Position = 0;

        if (reader.ReadBytesString(MAGIC_LENGTH, false) != WPD_MAGIC)
        {
            SharedMethods.ErrorExit("Not a valid WPD file");
        }

        // Skip one byte after magic
        reader.BaseStream.Position += 1;
    }

    /// <summary>
    /// Reads the record count from the file header.
    /// </summary>
    protected virtual void ReadRecordCount(BinaryReader reader, TVariables variables)
    {
        variables.RecordCount = reader.ReadBytesUInt32(true);

        if (variables.RecordCount == 0)
        {
            SharedMethods.ErrorExit("No records/sections are present in this file");
        }
    }

    /// <summary>
    /// Parses all sections from the WDB file.
    /// Subclasses can override to implement version-specific section parsing.
    /// </summary>
    /// <param name="reader">Binary reader positioned after header</param>
    /// <param name="variables">Variables to store section data</param>
    protected abstract void ParseSections(BinaryReader reader, TVariables variables);

    /// <summary>
    /// Writes the parsed data to JSON format.
    /// </summary>
    protected virtual void WriteToJson(BinaryReader wdbReader, TVariables variables)
    {
        using var jsonStream = new MemoryStream();
        var options = new JsonWriterOptions
        {
            Indented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using (var jsonWriter = new Utf8JsonWriter(jsonStream, options))
        {
            jsonWriter.WriteStartObject();

            WriteSectionsToJson(variables, jsonWriter);

            Log.Info("Parsing records....");
            ParseRecords(wdbReader, variables, jsonWriter);

            jsonWriter.WriteEndObject();
        }

        SaveJsonToFile(jsonStream, variables);
    }

    /// <summary>
    /// Writes section metadata to JSON.
    /// </summary>
    protected abstract void WriteSectionsToJson(TVariables variables, Utf8JsonWriter jsonWriter);

    /// <summary>
    /// Parses record data from the WDB file.
    /// </summary>
    protected abstract void ParseRecords(BinaryReader reader, TVariables variables, Utf8JsonWriter jsonWriter);

    /// <summary>
    /// Saves the JSON stream to a file.
    /// </summary>
    protected virtual void SaveJsonToFile(MemoryStream jsonStream, TVariables variables)
    {
        Log.Info("Writing wdb data to json file....");

        if (File.Exists(variables.JsonFilePath))
        {
            File.Delete(variables.JsonFilePath);
        }

        using var fileStream = File.Create(variables.JsonFilePath!);
        jsonStream.Position = 0;
        jsonStream.CopyTo(fileStream);

        Log.Info($"Extraction complete: {variables.JsonFilePath}");
    }
}
