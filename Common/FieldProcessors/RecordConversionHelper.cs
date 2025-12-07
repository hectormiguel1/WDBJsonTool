using System.Buffers.Binary;
using System.Text;
using WDBJsonTool.Extensions;
using WDBJsonTool.Support;

namespace WDBJsonTool.Common.FieldProcessors;

/// <summary>
/// Helper class for converting record field values from JSON to binary WDB format.
/// Provides common methods to reduce duplication in RecordsConversion classes.
/// </summary>
public static class RecordConversionHelper
{
    /// <summary>
    /// Packs a signed integer field into binary string for bitpacking.
    /// </summary>
    public static string PackIntField(int value, int fieldNum, ref int fieldBitsToProcess, ref string collectedBinary)
    {
        if (fieldNum != 0)
        {
            SharedMethods.ValidateInt(fieldNum, ref value);
        }

        if (fieldNum == 0)
        {
            fieldNum = 32;
        }

        if (fieldNum > fieldBitsToProcess)
        {
            return "overflow"; // Signal overflow
        }

        var binaryString = value.IntToBinaryFixed(fieldNum);

        if (binaryString.Length > fieldNum)
        {
            binaryString = binaryString.Substring(binaryString.Length - fieldNum, fieldNum);
        }

        binaryString = binaryString.ReverseBinary();
        collectedBinary += binaryString;
        fieldBitsToProcess -= fieldNum;

        return "success";
    }

    /// <summary>
    /// Packs an unsigned integer field into binary string for bitpacking.
    /// </summary>
    public static string PackUIntField(uint value, int fieldNum, ref int fieldBitsToProcess, ref string collectedBinary)
    {
        if (fieldNum != 0)
        {
            SharedMethods.ValidateUInt(fieldNum, ref value);
        }

        if (fieldNum == 0)
        {
            fieldNum = 32;
        }

        if (fieldNum > fieldBitsToProcess)
        {
            return "overflow"; // Signal overflow
        }

        var binaryString = value.UIntToBinaryFixed(fieldNum).ReverseBinary();
        collectedBinary += binaryString;
        fieldBitsToProcess -= fieldNum;

        return "success";
    }

    /// <summary>
    /// Packs a float field (as int) into binary string for bitpacking.
    /// </summary>
    public static string PackFloatField(int value, int fieldNum, ref int fieldBitsToProcess, ref string collectedBinary)
    {
        if (fieldNum != 0)
        {
            SharedMethods.ValidateInt(fieldNum, ref value);
        }

        if (fieldNum == 0)
        {
            fieldNum = 32;
        }

        if (fieldNum > fieldBitsToProcess)
        {
            return "overflow"; // Signal overflow
        }

        var binaryString = value.IntToBinaryFixed(fieldNum);

        if (binaryString.Length > fieldNum)
        {
            binaryString = binaryString.Substring(binaryString.Length - fieldNum, fieldNum);
        }

        binaryString = binaryString.ReverseBinary();
        collectedBinary += binaryString;
        fieldBitsToProcess -= fieldNum;

        return "success";
    }

    /// <summary>
    /// Packs a strArray field (uint index) into binary string for bitpacking.
    /// Used in XIII2LR for string array references.
    /// </summary>
    public static string PackStrArrayField(uint value, int fieldNum, ref int fieldBitsToProcess, ref string collectedBinary)
    {
        if (fieldNum != 0)
        {
            SharedMethods.ValidateUInt(fieldNum, ref value);
        }

        if (fieldNum == 0)
        {
            fieldNum = 32;
        }

        if (fieldNum > fieldBitsToProcess)
        {
            return "overflow"; // Signal overflow
        }

        var binaryString = value.UIntToBinaryFixed(fieldNum).ReverseBinary();
        collectedBinary += binaryString;
        fieldBitsToProcess -= fieldNum;

        return "success";
    }

    /// <summary>
    /// Writes collected bitpacked binary string to output byte array using BinaryPrimitives.
    /// </summary>
    public static void WriteBitpackedValue(byte[] outputArray, int offset, string collectedBinary)
    {
        collectedBinary = collectedBinary.ReverseBinary();
        var packedValue = Convert.ToUInt32(collectedBinary, 2);
        BinaryPrimitives.WriteUInt32BigEndian(outputArray.AsSpan(offset, 4), packedValue);
    }

    /// <summary>
    /// Writes a float value to output byte array using BinaryPrimitives.
    /// </summary>
    public static void WriteFloatValue(byte[] outputArray, int offset, float value)
    {
        BinaryPrimitives.WriteSingleBigEndian(outputArray.AsSpan(offset, 4), value);
    }

    /// <summary>
    /// Writes a uint32 value to output byte array using BinaryPrimitives.
    /// </summary>
    public static void WriteUInt32Value(byte[] outputArray, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(outputArray.AsSpan(offset, 4), value);
    }

    /// <summary>
    /// Writes a uint64 value to output byte array using BinaryPrimitives.
    /// </summary>
    public static void WriteUInt64Value(byte[] outputArray, int offset, ulong value)
    {
        BinaryPrimitives.WriteUInt64BigEndian(outputArray.AsSpan(offset, 8), value);
    }

    /// <summary>
    /// Processes a string field value, adds it to the strings dictionary if needed,
    /// and writes the offset to the output array.
    /// </summary>
    public static uint ProcessStringField(
        byte[] outputArray,
        int offset,
        string stringValue,
        Dictionary<string, uint> processedStringsDict,
        uint currentStringPos)
    {
        if (stringValue == "")
        {
            return currentStringPos; // No change to position
        }

        if (!processedStringsDict.ContainsKey(stringValue))
        {
            processedStringsDict.Add(stringValue, currentStringPos);
            WriteUInt32Value(outputArray, offset, currentStringPos);
            return currentStringPos + (uint)Encoding.UTF8.GetByteCount(stringValue + "\0");
        }
        else
        {
            WriteUInt32Value(outputArray, offset, processedStringsDict[stringValue]);
            return currentStringPos; // No change to position
        }
    }
}