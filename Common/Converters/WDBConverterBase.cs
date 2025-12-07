using System.Text;
using WDBJsonTool.Extensions;
using WDBJsonTool.Support;

namespace WDBJsonTool.Common.Converters;

/// <summary>
/// Abstract base class for WDB file converters (JSON to WDB).
/// Implements the template method pattern for common conversion flow.
/// </summary>
public abstract class WDBConverterBase<TVariables> where TVariables : WDBVariablesBase
{
    protected const string WPD_MAGIC = "WPD";
    protected const byte WPD_VERSION = 0x04;

    /// <summary>
    /// Main conversion method that orchestrates the building process.
    /// </summary>
    /// <param name="jsonFilePath">Path to the JSON file to convert</param>
    /// <param name="variables">Variables object to store conversion data</param>
    public void Convert(string jsonFilePath, TVariables variables)
    {
        InitializeVariables(jsonFilePath, variables);
        DeserializeJson(jsonFilePath, variables);

        Log.Info("Converting records....");
        ConvertRecords(variables);

        Log.Info("Building WDB file....");
        BuildWDBFile(variables);
    }

    /// <summary>
    /// Initializes variables with file-specific information.
    /// </summary>
    protected virtual void InitializeVariables(string jsonFilePath, TVariables variables)
    {
        var fileName = Path.GetFileNameWithoutExtension(jsonFilePath);
        var directory = Path.GetDirectoryName(jsonFilePath);
        variables.JsonFilePath = jsonFilePath;
        variables.WDBFilePath = Path.Combine(directory ?? ".", fileName + ".wdb");
    }

    /// <summary>
    /// Deserializes JSON data into the variables object.
    /// Subclasses implement version-specific deserialization.
    /// </summary>
    protected abstract void DeserializeJson(string jsonFilePath, TVariables variables);

    /// <summary>
    /// Converts record data from JSON format to binary format.
    /// </summary>
    protected abstract void ConvertRecords(TVariables variables);

    /// <summary>
    /// Builds the final WDB file from converted data.
    /// </summary>
    protected virtual void BuildWDBFile(TVariables variables)
    {
        using var wdbStream = File.Open(variables.WDBFilePath!, FileMode.Create, FileAccess.Write);
        using var wdbWriter = new BinaryWriter(wdbStream);

        WriteHeader(wdbWriter, variables);
        WriteSections(wdbWriter, variables);
        WriteRecords(wdbWriter, variables);

        Log.Info($"Conversion complete: {variables.WDBFilePath}");
    }

    /// <summary>
    /// Writes the WDB file header (magic, version, record count).
    /// </summary>
    protected virtual void WriteHeader(BinaryWriter writer, TVariables variables)
    {
        writer.Write(Encoding.UTF8.GetBytes(WPD_MAGIC));
        writer.Write(WPD_VERSION);
        writer.WriteBytesUInt32(variables.RecordCount, true);
    }

    /// <summary>
    /// Writes all sections to the WDB file.
    /// Subclasses implement version-specific section writing.
    /// </summary>
    protected abstract void WriteSections(BinaryWriter writer, TVariables variables);

    /// <summary>
    /// Writes record data to the WDB file.
    /// </summary>
    protected abstract void WriteRecords(BinaryWriter writer, TVariables variables);
}
