using System.Buffers.Binary;
using System.Text;
using WDBJsonTool.Extensions;

namespace WDBJsonTool.Support;

/// <summary>
/// Provides shared utility methods for common operations across WDB processing.
/// Includes binary data extraction, string parsing, validation, and error handling.
/// </summary>
internal static class SharedMethods
{
    /// <summary>
    /// Logs an error message and throws an InvalidOperationException.
    /// Used to terminate processing when encountering unrecoverable errors.
    /// </summary>
    /// <param name="errorMsg">The error message to log and include in the exception</param>
    /// <exception cref="InvalidOperationException">Always thrown with the provided error message</exception>
    public static void ErrorExit(string errorMsg)
    {
        Log.Error(errorMsg);
        throw new InvalidOperationException(errorMsg);
    }

    /// <summary>
    /// Validates that the current position in the binary reader contains the expected section name.
    /// Reads 16 bytes and compares to the expected section name.
    /// </summary>
    /// <param name="br">The binary reader positioned at the start of a section header</param>
    /// <param name="sectionName">The expected section name (e.g., "!!string", "!!version")</param>
    /// <exception cref="InvalidOperationException">Thrown if the section name doesn't match</exception>
    public static void CheckSectionName(BinaryReader br, string sectionName)
    {
        if (br.ReadBytesString(16, false) != sectionName)
        {
            ErrorExit($"{sectionName} is not present in the expected position");
        }
    }

    /// <summary>
    /// Reads section data from a WDB file based on the section header.
    /// Extracts offset and length from the header, seeks to the data location, and reads the bytes.
    /// </summary>
    /// <param name="br">The binary reader positioned after the section name (at offset/length)</param>
    /// <param name="reverse">If true, reverses the byte array before returning</param>
    /// <returns>Byte array containing the section data</returns>
    public static byte[] SaveSectionData(BinaryReader br, bool reverse)
    {
        var sectionOffset = br.ReadBytesUInt32(true);
        var sectionLength = br.ReadBytesUInt32(true);

        _ = br.BaseStream.Position = sectionOffset;
        var sectionData = br.ReadBytes((int)sectionLength);

        if (reverse)
        {
            Array.Reverse(sectionData);
        }

        return sectionData;
    }

    /// <summary>
    /// Parses a byte array into a list of uint32 values.
    /// Assumes the array length is a multiple of 4 (big-endian uint32 values).
    /// </summary>
    /// <param name="dataArray">Byte array containing packed uint32 values</param>
    /// <returns>List of uint values extracted from the array</returns>
    public static List<uint> GetSectionDataValues(byte[] dataArray)
    {
        var processList = new List<uint>();
        var dataIndex = 0;

        for (var i = 0; i < dataArray.Length / 4; i++)
        {
            var currentValue = DeriveUIntFromSectionData(dataArray, dataIndex, true);
            processList.Add(currentValue);

            dataIndex += 4;
        }

        return processList;
    }

    /// <summary>
    /// Extracts a null-terminated UTF-8 string from a byte array at the specified offset.
    /// Reads bytes until encountering a null terminator (0x00).
    /// </summary>
    /// <param name="dataArray">Byte array containing string data (typically from !!string section)</param>
    /// <param name="stringOffset">Byte offset where the string begins</param>
    /// <returns>The extracted UTF-8 string without the null terminator</returns>
    public static string DeriveStringFromArray(byte[] dataArray, int stringOffset)
    {
        var length = 0;
        for (var s = stringOffset; s < dataArray.Length; s++)
        {
            if (dataArray[s] == 0)
            {
                break;
            }

            length++;
        }

        return Encoding.UTF8.GetString(dataArray, stringOffset, length);
    }

    /// <summary>
    /// Extracts the numeric bit count from a field name.
    /// Field names follow the pattern: [prefix][digits] (e.g., "i8", "u16", "f32").
    /// </summary>
    /// <param name="fieldName">The field name (e.g., "i8health", "u32id")</param>
    /// <returns>The numeric portion (e.g., 8, 32), or 0 if no digits found</returns>
    public static int DeriveFieldNumber(string fieldName)
    {
        var foundNumsList = new List<int>();

        for (var i = 1; i < 3; i++)
        {
            if (i == 1 && !char.IsDigit(fieldName[i]))
            {
                break;
            }

            if (char.IsDigit(fieldName[i]))
            {
                foundNumsList.Add(int.Parse(Convert.ToString(fieldName[i])));
            }
        }

        var foundNumStr = foundNumsList.Aggregate("", (current, n) => current + n);

        var hasParsed = int.TryParse(foundNumStr, out var foundNum);

        return hasParsed ? foundNum : 0;
    }

    /// <summary>
    /// Extracts a 32-bit unsigned integer from a byte array at the specified index.
    /// </summary>
    /// <param name="dataArray">Source byte array</param>
    /// <param name="dataArrayIndex">Starting index in the array</param>
    /// <param name="reverse">If true, reverses bytes (big-endian to little-endian conversion)</param>
    /// <returns>The extracted uint32 value</returns>
    public static uint DeriveUIntFromSectionData(byte[] dataArray, int dataArrayIndex, bool reverse)
    {
        ReadOnlySpan<byte> span = dataArray.AsSpan(dataArrayIndex, 4);
        return reverse
            ? BinaryPrimitives.ReadUInt32BigEndian(span)
            : BinaryPrimitives.ReadUInt32LittleEndian(span);
    }

    /// <summary>
    /// Extracts a 32-bit IEEE 754 float from a byte array at the specified index.
    /// </summary>
    /// <param name="dataArray">Source byte array</param>
    /// <param name="dataArrayIndex">Starting index in the array</param>
    /// <param name="reverse">If true, reverses bytes (big-endian to little-endian conversion)</param>
    /// <returns>The extracted float value</returns>
    public static float DeriveFloatFromSectionData(byte[] dataArray, int dataArrayIndex, bool reverse)
    {
        ReadOnlySpan<byte> span = dataArray.AsSpan(dataArrayIndex, 4);
        return reverse
            ? BinaryPrimitives.ReadSingleBigEndian(span)
            : BinaryPrimitives.ReadSingleLittleEndian(span);
    }

    /// <summary>
    /// Converts a list of integers to a byte array with specified value size.
    /// Handles big-endian conversion for multi-byte values.
    /// </summary>
    /// <param name="intList">List of integer values to convert</param>
    /// <param name="perValueSize">Size in bytes per value (1 or 4 supported)</param>
    /// <returns>Byte array containing the packed integer values</returns>
    public static byte[] CreateArrayFromIntList(List<int> intList, int perValueSize)
    {
        var count = intList.Count;
        var dataArray = new byte[perValueSize * count];
        var index = 0;

        for (var i = 0; i < count; i++)
        {
            switch (perValueSize)
            {
                case 1:
                    dataArray[i] = (byte)intList[i];
                    break;

                case 4:
                    BinaryPrimitives.WriteUInt32BigEndian(dataArray.AsSpan(index, 4), (uint)intList[i]);
                    index += 4;
                    break;
            }
        }

        return dataArray;
    }

    /// <summary>
    /// Converts a list of unsigned integers to a big-endian byte array.
    /// Each uint32 value is converted to 4 bytes in big-endian order.
    /// </summary>
    /// <param name="uintList">List of uint values to convert</param>
    /// <returns>Byte array containing the packed uint values (4 bytes each)</returns>
    public static byte[] CreateArrayFromUIntList(List<uint> uintList)
    {
        var count = uintList.Count;
        var dataArray = new byte[4 * count];
        var index = 0;

        for (var i = 0; i < count; i++)
        {
            BinaryPrimitives.WriteUInt32BigEndian(dataArray.AsSpan(index, 4), uintList[i]);
            index += 4;
        }

        return dataArray;
    }

    /// <summary>
    /// Validates that an unsigned integer value fits within the specified bit width.
    /// If the value exceeds the maximum for the bit width, it's set to 0 and a warning is logged.
    /// </summary>
    /// <param name="fieldNum">Number of bits available for the value</param>
    /// <param name="value">Reference to the value to validate (modified if out of range)</param>
    public static void ValidateUInt(int fieldNum, ref uint value)
    {
        var maxValue = Convert.ToUInt32(new string('1', fieldNum), 2);

        if (value <= maxValue) return;
        Log.Warn($"Value {value} will be zeroed due to exceeding bit amount");
        value = 0;
    }


    public static void ValidateInt(int fieldNum, ref int value)
    {
        if (value < 0)
        {
            var valueBinary = Convert.ToString(value, 2);
            valueBinary = valueBinary.Substring(valueBinary.Length - fieldNum, fieldNum);

            var newValue = valueBinary.BinaryToInt(0, fieldNum);

            if (newValue == value) return;
        }
        else
        {
            var maxValue = Convert.ToInt32(new string('1', fieldNum), 2);

            if (value <= maxValue) return;
        }

        Log.Warn($"Value {value} will be zeroed due to exceeding bit amount");
        value = 0;
    }
}