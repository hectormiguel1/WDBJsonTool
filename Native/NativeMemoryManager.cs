using System.Runtime.InteropServices;
using System.Text;

namespace WDBJsonTool.Native
{
    /// <summary>
    /// Manages native memory allocations for interop
    /// </summary>
    public static unsafe class NativeMemoryManager
    {
        /// <summary>
        /// Allocates a UTF-8 string in native memory
        /// </summary>
        public static IntPtr AllocateString(string? str)
        {
            if (string.IsNullOrEmpty(str))
            {
                var emptyPtr = Marshal.AllocHGlobal(1);
                Marshal.WriteByte(emptyPtr, 0);
                return emptyPtr;
            }

            var bytes = Encoding.UTF8.GetBytes(str);
            var ptr = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, ptr, bytes.Length);
            Marshal.WriteByte(ptr + bytes.Length, 0); // Null terminator
            return ptr;
        }

        /// <summary>
        /// Copies a string to a fixed-size buffer
        /// </summary>
        public static void CopyStringToFixedBuffer(byte* buffer, int bufferSize, string? str)
        {
            if (string.IsNullOrEmpty(str))
            {
                buffer[0] = 0;
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(str);
            var copyLength = Math.Min(bytes.Length, bufferSize - 1);

            for (int i = 0; i < copyLength; i++)
            {
                buffer[i] = bytes[i];
            }
            buffer[copyLength] = 0; // Null terminator
        }

        /// <summary>
        /// Allocates an array of pointers to strings
        /// </summary>
        public static IntPtr AllocateStringArray(string[]? strings, out int count)
        {
            if (strings == null || strings.Length == 0)
            {
                count = 0;
                return IntPtr.Zero;
            }

            count = strings.Length;
            var arrayPtr = Marshal.AllocHGlobal(IntPtr.Size * strings.Length);
            var ptrArray = (IntPtr*)arrayPtr;

            for (int i = 0; i < strings.Length; i++)
            {
                ptrArray[i] = AllocateString(strings[i]);
            }

            return arrayPtr;
        }

        /// <summary>
        /// Allocates an array of uint values
        /// </summary>
        public static IntPtr AllocateUIntArray(List<uint> values)
        {
            if (values == null || values.Count == 0)
                return IntPtr.Zero;

            var ptr = Marshal.AllocHGlobal(sizeof(uint) * values.Count);
            var uintPtr = (uint*)ptr;

            for (int i = 0; i < values.Count; i++)
            {
                uintPtr[i] = values[i];
            }

            return ptr;
        }

        /// <summary>
        /// Allocates an array of int values
        /// </summary>
        public static IntPtr AllocateIntArray(List<int> values)
        {
            if (values == null || values.Count == 0)
                return IntPtr.Zero;

            var ptr = Marshal.AllocHGlobal(sizeof(int) * values.Count);
            var intPtr = (int*)ptr;

            for (int i = 0; i < values.Count; i++)
            {
                intPtr[i] = values[i];
            }

            return ptr;
        }

        /// <summary>
        /// Free a WdbFile structure and all its allocated memory
        /// </summary>
        public static void FreeWdbFile(WdbFile* wdbFile)
        {
            if (wdbFile == null)
                return;

            // Free records
            if (wdbFile->Records != IntPtr.Zero)
            {
                var records = (WdbRecord*)wdbFile->Records;
                for (uint i = 0; i < wdbFile->RecordCount; i++)
                {
                    FreeWdbRecord(&records[i]);
                }
                Marshal.FreeHGlobal(wdbFile->Records);
            }

            // Free sections
            if (wdbFile->Sections != IntPtr.Zero)
            {
                var sections = (WdbSection*)wdbFile->Sections;
                for (uint i = 0; i < wdbFile->SectionCount; i++)
                {
                    if (sections[i].Data != IntPtr.Zero)
                        Marshal.FreeHGlobal(sections[i].Data);
                }
                Marshal.FreeHGlobal(wdbFile->Sections);
            }

            // Free field names
            if (wdbFile->FieldNames != IntPtr.Zero)
            {
                var fieldNames = (IntPtr*)wdbFile->FieldNames;
                for (uint i = 0; i < wdbFile->FieldDefinitionCount; i++)
                {
                    if (fieldNames[i] != IntPtr.Zero)
                        Marshal.FreeHGlobal(fieldNames[i]);
                }
                Marshal.FreeHGlobal(wdbFile->FieldNames);
            }

            // Free strtypelist values
            if (wdbFile->StrtypelistValues != IntPtr.Zero)
                Marshal.FreeHGlobal(wdbFile->StrtypelistValues);

            // Free the WdbFile struct itself
            Marshal.FreeHGlobal((IntPtr)wdbFile);
        }

        /// <summary>
        /// Free a WdbRecord and its fields
        /// </summary>
        public static void FreeWdbRecord(WdbRecord* record)
        {
            if (record == null || record->Fields == IntPtr.Zero)
                return;

            var fields = (WdbField*)record->Fields;
            for (uint i = 0; i < record->FieldCount; i++)
            {
                if (fields[i].Value.Type == WdbFieldType.String && fields[i].Value.StringPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(fields[i].Value.StringPtr);
                }
            }
            Marshal.FreeHGlobal(record->Fields);
        }

        /// <summary>
        /// Free a WdbParseResult
        /// </summary>
        public static void FreeParseResult(WdbParseResult* result)
        {
            if (result == null)
                return;

            if (result->ErrorMessage != IntPtr.Zero)
                Marshal.FreeHGlobal(result->ErrorMessage);

            if (result->WdbFile != IntPtr.Zero)
                FreeWdbFile((WdbFile*)result->WdbFile);

            Marshal.FreeHGlobal((IntPtr)result);
        }

        /// <summary>
        /// Free a WdbFileXIII2LR structure
        /// </summary>
        public static void FreeWdbFileXIII2LR(WdbFileXIII2LR* wdbFile)
        {
            if (wdbFile == null)
                return;

            // Free records
            if (wdbFile->Records != IntPtr.Zero)
            {
                var records = (WdbRecord*)wdbFile->Records;
                for (uint i = 0; i < wdbFile->RecordCount; i++)
                {
                    FreeWdbRecord(&records[i]);
                }
                Marshal.FreeHGlobal(wdbFile->Records);
            }

            // Free sections
            if (wdbFile->Sections != IntPtr.Zero)
            {
                var sections = (WdbSection*)wdbFile->Sections;
                for (uint i = 0; i < wdbFile->SectionCount; i++)
                {
                    if (sections[i].Data != IntPtr.Zero)
                        Marshal.FreeHGlobal(sections[i].Data);
                }
                Marshal.FreeHGlobal(wdbFile->Sections);
            }

            // Free field names
            if (wdbFile->FieldNames != IntPtr.Zero)
            {
                var fieldNames = (IntPtr*)wdbFile->FieldNames;
                for (uint i = 0; i < wdbFile->FieldDefinitionCount; i++)
                {
                    if (fieldNames[i] != IntPtr.Zero)
                        Marshal.FreeHGlobal(fieldNames[i]);
                }
                Marshal.FreeHGlobal(wdbFile->FieldNames);
            }

            // Free strtypelist values
            if (wdbFile->StrtypelistValues != IntPtr.Zero)
                Marshal.FreeHGlobal(wdbFile->StrtypelistValues);

            // Free strArray entries
            if (wdbFile->StrArrayEntries != IntPtr.Zero)
            {
                var entries = (WdbStrArrayEntry*)wdbFile->StrArrayEntries;
                for (uint i = 0; i < wdbFile->StrArrayEntryCount; i++)
                {
                    if (entries[i].Strings != IntPtr.Zero)
                    {
                        var strings = (IntPtr*)entries[i].Strings;
                        for (uint j = 0; j < entries[i].StringCount; j++)
                        {
                            if (strings[j] != IntPtr.Zero)
                                Marshal.FreeHGlobal(strings[j]);
                        }
                        Marshal.FreeHGlobal(entries[i].Strings);
                    }
                }
                Marshal.FreeHGlobal(wdbFile->StrArrayEntries);
            }

            Marshal.FreeHGlobal((IntPtr)wdbFile);
        }
    }
}
