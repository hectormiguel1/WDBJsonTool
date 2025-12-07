using System.Buffers.Binary;
using WDBJsonTool.Extensions;
using WDBJsonTool.Support;

namespace WDBJsonTool.Common.FieldProcessors;

/// <summary>
/// Processes bitpacked fields that contain multiple values packed into 32 bits.
/// Handles signed/unsigned integers, floats, and string array offsets.
/// </summary>
public class BitpackedFieldProcessor : IBitpackedFieldProcessor
{
    /// <summary>
    /// Extracts individual field values from a 32-bit packed value.
    /// </summary>
    /// <param name="packedValue">The 32-bit value containing packed fields</param>
    /// <param name="fieldDefinitions">Field names defining types and bit widths (e.g., "i8health", "u16id")</param>
    /// <returns>Dictionary mapping field names to their extracted values</returns>
    public Dictionary<string, object> ExtractBitpackedFields(uint packedValue, string[] fieldDefinitions)
    {
        var result = new Dictionary<string, object>();
        var binaryData = packedValue.UIntToBinary();
        var binaryDataIndex = binaryData.Length;
        var fieldBitsToProcess = 32;
        var fieldIndex = 0;

        while (fieldBitsToProcess > 0 && fieldIndex < fieldDefinitions.Length)
        {
            var fieldName = fieldDefinitions[fieldIndex];
            var fieldType = fieldName[0]; // 'i', 'u', 'f', 's'
            var fieldNum = SharedMethods.DeriveFieldNumber(fieldName);

            // Handle full 32-bit field
            if (fieldNum == 0)
            {
                fieldNum = 32;
            }

            // Check if field fits in remaining bits
            if (fieldNum > fieldBitsToProcess)
            {
                // Field spans multiple 32-bit words - caller needs to handle this
                break;
            }

            // Extract the field value
            binaryDataIndex -= fieldNum;
            object value = fieldType switch
            {
                'i' => binaryData.BinaryToInt(binaryDataIndex, fieldNum),
                'u' => (int)binaryData.BinaryToUInt(binaryDataIndex, fieldNum),
                'f' => binaryData.BinaryToInt(binaryDataIndex, fieldNum), // Float stored as int in bitpacked
                's' => (int)binaryData.BinaryToUInt(binaryDataIndex, fieldNum), // String array offset
                _ => throw new InvalidOperationException($"Unknown field type '{fieldType}' in field '{fieldName}'")
            };

            result[fieldName] = value;
            fieldBitsToProcess -= fieldNum;
            fieldIndex++;
        }

        return result;
    }

    /// <summary>
    /// Packs multiple field values into a single 32-bit value.
    /// </summary>
    /// <param name="fieldValues">Dictionary mapping field names to their values</param>
    /// <param name="fieldDefinitions">Field names defining the packing order and bit widths</param>
    /// <returns>The packed 32-bit value</returns>
    public uint PackFields(Dictionary<string, object> fieldValues, string[] fieldDefinitions)
    {
        var collectedBinary = string.Empty;
        var fieldBitsToProcess = 32;
        var fieldIndex = 0;

        while (fieldBitsToProcess > 0 && fieldIndex < fieldDefinitions.Length)
        {
            var fieldName = fieldDefinitions[fieldIndex];
            var fieldType = fieldName[0];
            var fieldNum = SharedMethods.DeriveFieldNumber(fieldName);

            if (fieldNum == 0)
            {
                fieldNum = 32;
            }

            if (fieldNum > fieldBitsToProcess)
            {
                // Field spans multiple 32-bit words
                break;
            }

            if (!fieldValues.TryGetValue(fieldName, out var fieldValue))
            {
                throw new InvalidOperationException($"Missing value for field '{fieldName}'");
            }

            var fieldBinary = fieldType switch
            {
                'i' => PackSignedInt(Convert.ToInt32(fieldValue), fieldNum),
                'u' => Convert.ToUInt32(fieldValue).UIntToBinaryFixed(fieldNum),
                'f' => PackSignedInt(Convert.ToInt32(fieldValue), fieldNum), // Float as int in bitpacked
                's' => Convert.ToUInt32(fieldValue).UIntToBinaryFixed(fieldNum), // String array offset
                _ => throw new InvalidOperationException($"Unknown field type '{fieldType}' in field '{fieldName}'")
            };

            // Reverse the binary string for this field
            fieldBinary = fieldBinary.ReverseBinary();
            collectedBinary += fieldBinary;

            fieldBitsToProcess -= fieldNum;
            fieldIndex++;
        }

        // Reverse the entire collected binary and convert to uint
        collectedBinary = collectedBinary.ReverseBinary();
        return Convert.ToUInt32(collectedBinary, 2);
    }

    /// <summary>
    /// Packs a signed integer, handling negative values correctly.
    /// </summary>
    private static string PackSignedInt(int value, int bitWidth)
    {
        var binary = value.IntToBinaryFixed(bitWidth);

        // Ensure the binary string is the correct length
        if (binary.Length > bitWidth)
        {
            binary = binary.Substring(binary.Length - bitWidth, bitWidth);
        }

        return binary;
    }

    /// <summary>
    /// Reads a packed 32-bit value from binary data.
    /// </summary>
    public static uint ReadPackedValue(ReadOnlySpan<byte> data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
    }

    /// <summary>
    /// Writes a packed 32-bit value to binary data.
    /// </summary>
    public void WritePackedValue(Span<byte> data, int offset, uint packedValue)
    {
        BinaryPrimitives.WriteUInt32BigEndian(data.Slice(offset, 4), packedValue);
    }
}
