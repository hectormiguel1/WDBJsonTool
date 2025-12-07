namespace WDBJsonTool.Common.FieldProcessors;

/// <summary>
/// Base interface for field processors that can extract and write field values.
/// </summary>
/// <typeparam name="T">The type of value this processor handles</typeparam>
public interface IFieldProcessor<T>
{
    /// <summary>
    /// Extracts a value from binary data at the specified offset.
    /// </summary>
    /// <param name="data">The source data span</param>
    /// <param name="offset">Offset within the span to read from</param>
    /// <returns>The extracted value</returns>
    T Extract(ReadOnlySpan<byte> data, int offset);

    /// <summary>
    /// Writes a value to binary data at the specified offset.
    /// </summary>
    /// <param name="data">The destination data span</param>
    /// <param name="offset">Offset within the span to write to</param>
    /// <param name="value">The value to write</param>
    void Write(Span<byte> data, int offset, T value);

    /// <summary>
    /// Gets the size in bytes that this field occupies.
    /// </summary>
    int SizeInBytes { get; }
}

/// <summary>
/// Interface for processors that handle bitpacked fields.
/// Bitpacked fields contain multiple values packed into 32 bits.
/// </summary>
public interface IBitpackedFieldProcessor
{
    /// <summary>
    /// Extracts bitpacked values from a 32-bit value based on field definitions.
    /// </summary>
    /// <param name="packedValue">The 32-bit packed value</param>
    /// <param name="fieldDefinitions">Array of field names (e.g., "i8health", "u16id")</param>
    /// <returns>Dictionary mapping field names to their extracted values</returns>
    Dictionary<string, object> ExtractBitpackedFields(uint packedValue, string[] fieldDefinitions);

    /// <summary>
    /// Packs multiple field values into a single 32-bit value.
    /// </summary>
    /// <param name="fieldValues">Dictionary mapping field names to values</param>
    /// <param name="fieldDefinitions">Array of field names defining the packing order</param>
    /// <returns>The packed 32-bit value</returns>
    uint PackFields(Dictionary<string, object> fieldValues, string[] fieldDefinitions);
}