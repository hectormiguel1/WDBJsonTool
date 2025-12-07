using System.Text.Json;
using WDBJsonTool.Extensions;

namespace WDBJsonTool.Common.FieldProcessors;

/// <summary>
/// Helper class to simplify using field processors with existing WDB parsing code.
/// </summary>
public static class FieldProcessorHelper
{
    private static readonly FloatFieldProcessor _floatProcessor = new();
    private static readonly UIntFieldProcessor _uint32Processor = new(false);
    private static readonly UIntFieldProcessor _uint64Processor = new(true);
    private static readonly StringOffsetFieldProcessor _stringOffsetProcessor = new();
    private static readonly BitpackedFieldProcessor _bitpackedProcessor = new();

    /// <summary>
    /// Extracts a float field from record data and writes it to JSON.
    /// </summary>
    public static void ProcessFloatField(ReadOnlySpan<byte> data, int offset, string fieldName, Utf8JsonWriter jsonWriter)
    {
        var value = _floatProcessor.Extract(data, offset);
        Log.Debug($"{fieldName}: {value}");
        jsonWriter.WriteNumber(fieldName, value);
    }

    /// <summary>
    /// Extracts a uint field from record data and writes it to JSON.
    /// </summary>
    public static void ProcessUIntField(ReadOnlySpan<byte> data, int offset, string fieldName, Utf8JsonWriter jsonWriter, bool is64Bit = false)
    {
        var processor = is64Bit ? _uint64Processor : _uint32Processor;
        var value = processor.Extract(data, offset);

        Log.Debug($"{fieldName}: {value}");

        if (is64Bit)
        {
            jsonWriter.WriteNumber(fieldName, value);
        }
        else
        {
            jsonWriter.WriteNumber(fieldName, (uint)value);
        }
    }

    /// <summary>
    /// Extracts a string offset field from record data and returns the offset value.
    /// </summary>
    public static uint ExtractStringOffset(ReadOnlySpan<byte> data, int offset)
    {
        return _stringOffsetProcessor.Extract(data, offset);
    }

    /// <summary>
    /// Extracts bitpacked fields from a 32-bit packed value and writes them to JSON.
    /// Returns the number of fields that were successfully extracted.
    /// </summary>
    public static int ProcessBitpackedFields(
        ReadOnlySpan<byte> data,
        int offset,
        string[] fieldNames,
        int startFieldIndex,
        Utf8JsonWriter jsonWriter)
    {
        // Read the packed 32-bit value
        var packedValue = BitpackedFieldProcessor.ReadPackedValue(data, offset);

        // Determine which fields fit in this 32-bit word
        var fieldsInWord = GetFieldsInWord(fieldNames, startFieldIndex);

        // Extract all fields from the packed value
        var extractedFields = _bitpackedProcessor.ExtractBitpackedFields(packedValue, fieldsInWord);

        // Write each field to JSON
        foreach (var (fieldName, value) in extractedFields)
        {
            Log.Debug($"{fieldName}: {value}");

            switch (value)
            {
                case int intValue:
                    jsonWriter.WriteNumber(fieldName, intValue);
                    break;
                case uint uintValue:
                    jsonWriter.WriteNumber(fieldName, uintValue);
                    break;
            }
        }

        return extractedFields.Count;
    }

    /// <summary>
    /// Determines which consecutive fields starting from startIndex fit within a 32-bit word.
    /// </summary>
    private static string[] GetFieldsInWord(string[] allFields, int startIndex)
    {
        var fieldsInWord = new List<string>();
        var bitsUsed = 0;

        for (var i = startIndex; i < allFields.Length; i++)
        {
            var fieldName = allFields[i];
            var fieldBits = Support.SharedMethods.DeriveFieldNumber(fieldName);

            if (fieldBits == 0) fieldBits = 32;

            if (bitsUsed + fieldBits > 32)
            {
                break; // This field would overflow the 32-bit word
            }

            fieldsInWord.Add(fieldName);
            bitsUsed += fieldBits;

            if (bitsUsed == 32)
            {
                break; // Word is full
            }
        }

        return fieldsInWord.ToArray();
    }
}