namespace WDBJsonTool.Support.Constants
{
    /// <summary>
    /// Constants representing field data types in the WDB strtypelist section.
    /// These values determine how to interpret the binary data for each field.
    /// </summary>
    public static class FieldTypeConstants
    {
        /// <summary>
        /// Bitpacked data type (value: 0).
        /// Fields are packed into 32-bit chunks with variable bit widths.
        /// Individual fields are identified by prefixes: i# (signed int), u# (unsigned int), f# (float), s# (string array).
        /// </summary>
        public const uint BITPACKED = 0;

        /// <summary>
        /// 32-bit floating point value (value: 1).
        /// Stored as a standard IEEE 754 single-precision float.
        /// </summary>
        public const uint FLOAT = 1;

        /// <summary>
        /// String offset reference (value: 2).
        /// Contains a 32-bit offset pointing into the !!string section.
        /// The offset is the byte position where the null-terminated string begins.
        /// </summary>
        public const uint STRING_OFFSET = 2;

        /// <summary>
        /// 32-bit unsigned integer value (value: 3).
        /// Stored as a standard 32-bit uint (or 64-bit if field name starts with "u64").
        /// </summary>
        public const uint UINT = 3;
    }

    /// <summary>
    /// Constants for field type prefixes used in bitpacked data.
    /// These single-character prefixes appear in field names to identify the data type.
    /// </summary>
    public static class FieldPrefixConstants
    {
        /// <summary>
        /// Signed integer field prefix (e.g., "i8", "i16", "i32").
        /// The number indicates bit width (0 or 32 means full 32-bit).
        /// </summary>
        public const string SIGNED_INT = "i";

        /// <summary>
        /// Unsigned integer field prefix (e.g., "u8", "u16", "u32").
        /// The number indicates bit width (0 or 32 means full 32-bit).
        /// </summary>
        public const string UNSIGNED_INT = "u";

        /// <summary>
        /// Float field prefix (e.g., "f8", "f16", "f32").
        /// Stored as bitpacked integer, the number indicates bit width.
        /// </summary>
        public const string FLOAT = "f";

        /// <summary>
        /// String array field prefix (e.g., "s8", "s16") - XIII2LR only.
        /// The number indicates bit width for the array index value.
        /// The index references into the !!strArray section.
        /// </summary>
        public const string STRING_ARRAY = "s";
    }

    /// <summary>
    /// Constants for WDB file structure and format.
    /// </summary>
    public static class WDBFormatConstants
    {
        /// <summary>
        /// Magic bytes at the start of every WDB file: "WPD" (0x57 0x50 0x44).
        /// Used to validate file format.
        /// </summary>
        public const string MAGIC_HEADER = "WPD";

        /// <summary>
        /// Size of the file header in bytes.
        /// Header contains magic bytes, record count, and other metadata.
        /// </summary>
        public const int HEADER_SIZE = 16;

        /// <summary>
        /// Size of each section header in bytes.
        /// Section headers contain section name and data length.
        /// </summary>
        public const int SECTION_HEADER_SIZE = 16;

        /// <summary>
        /// Size of each record header in bytes.
        /// Record headers contain the record name (null-terminated string).
        /// </summary>
        public const int RECORD_HEADER_SIZE = 16;

        /// <summary>
        /// Size of data per record entry in bytes.
        /// Record data follows the header in 16-byte aligned blocks.
        /// </summary>
        public const int RECORD_DATA_SIZE = 16;

        /// <summary>
        /// Standard size for bitpacked data chunks in bits.
        /// Most bitpacked fields are processed in 32-bit chunks.
        /// </summary>
        public const int BITPACKED_CHUNK_BITS = 32;

        /// <summary>
        /// Size of a standard field value in bytes (uint32, float, offset).
        /// </summary>
        public const int FIELD_VALUE_SIZE = 4;

        /// <summary>
        /// Size of a 64-bit field value in bytes.
        /// Used for u64 fields.
        /// </summary>
        public const int FIELD_VALUE_SIZE_64 = 8;
    }
}
