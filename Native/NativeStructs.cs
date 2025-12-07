using System.Runtime.InteropServices;

namespace WDBJsonTool.Native;
/// <summary>
/// Field value types matching the WDB strtypelist values
/// </summary>
public enum WdbFieldType : int
{
    Bitpacked = 0,
    Float = 1,
    String = 2,
    UInt = 3
}

/// <summary>
/// A single field value that can hold different types
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public unsafe struct WdbFieldValue
{
    [FieldOffset(0)] public WdbFieldType Type;

    // Union of possible values (offset 4 to align after Type)
    [FieldOffset(4)] public int IntValue;
    [FieldOffset(4)] public uint UIntValue;
    [FieldOffset(4)] public ulong UInt64Value;
    [FieldOffset(4)] public float FloatValue;
    [FieldOffset(4)] public IntPtr StringPtr; // Pointer to null-terminated UTF-8 string
}

/// <summary>
/// A named field with its value
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WdbField
{
    public fixed byte Name[64]; // Field name (null-terminated UTF-8)
    public WdbFieldValue Value;
}

/// <summary>
/// A single record containing multiple fields
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WdbRecord
{
    public fixed byte Name[16]; // Record name (null-terminated)
    public uint FieldCount;
    public IntPtr Fields; // Pointer to array of WdbField
}

/// <summary>
/// Section metadata
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WdbSection
{
    public fixed byte Name[32]; // Section name (null-terminated)
    public uint DataSize;
    public IntPtr Data; // Pointer to raw section data
}

/// <summary>
/// Complete parsed WDB file structure
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WdbFile
{
    public fixed byte SheetName[64]; // Sheet name (null-terminated)
    public uint RecordCount;
    public uint SectionCount;
    public uint FieldDefinitionCount;
    public IntPtr Records; // Pointer to array of WdbRecord
    public IntPtr Sections; // Pointer to array of WdbSection (for raw section data like !!string, !!typelist, etc.)
    public IntPtr FieldNames; // Pointer to array of field name pointers (for known WDBs)
    public IntPtr StrtypelistValues; // Pointer to array of uint (field types)
    public byte IsKnown; // true (1) if known WDB with field names, false (0) otherwise
}

/// <summary>
/// Result structure for parse operations
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WdbParseResult
{
    public int Success; // 0 = success, non-zero = error code
    public IntPtr ErrorMessage; // Pointer to error message if failed (null-terminated UTF-8)
    public IntPtr WdbFile; // Pointer to WdbFile if successful
}

/// <summary>
/// String array entry for XIII-2/LR
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WdbStrArrayEntry
{
    public fixed byte Key[64]; // Field key name
    public uint StringCount;
    public IntPtr Strings; // Pointer to array of string pointers
}

/// <summary>
/// Extended WDB file structure for XIII-2/LR with strArray support
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct WdbFileXIII2LR
{
    public fixed byte SheetName[64];
    public uint RecordCount;
    public uint SectionCount;
    public uint FieldDefinitionCount;
    public IntPtr Records;
    public IntPtr Sections;
    public IntPtr FieldNames;
    public IntPtr StrtypelistValues;
    public byte HasStrArraySection; // true (1) if strArray section present, false (0) otherwise
    public uint StrArrayEntryCount;
    public IntPtr StrArrayEntries; // Pointer to array of WdbStrArrayEntry
}
