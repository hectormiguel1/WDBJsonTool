namespace WDBJsonTool.Common;

/// <summary>
/// Field value types matching the WDB strtypelist values.
/// These correspond to the field type indicators in the WDB file format.
/// </summary>
public enum WdbFieldType : int
{
    /// <summary>
    /// Bitpacked field - multiple small values packed into 32 bits.
    /// Used for signed ints, unsigned ints, floats, and string arrays.
    /// </summary>
    Bitpacked = 0,

    /// <summary>
    /// Full 32-bit IEEE 754 floating-point value.
    /// </summary>
    Float = 1,

    /// <summary>
    /// String offset - 32-bit offset into the !!string section.
    /// </summary>
    String = 2,

    /// <summary>
    /// Full 32-bit or 64-bit unsigned integer value.
    /// </summary>
    UInt = 3
}
