using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using WDBJsonTool.DataStructures;
using WDBJsonTool.Support; // For Log.Info
using WDBJsonTool.XIII.Extraction; // For XIII parsing logic
using WDBJsonTool.XIII2LR.Extraction; // For XIII2LR parsing logic

namespace WDBJsonTool.Native
{
    // Equivalent to the C enum WDBValueType
    internal enum WDBValueType
    {
        WDB_VALUE_TYPE_INT,
        WDB_VALUE_TYPE_UINT,
        WDB_VALUE_TYPE_FLOAT,
        WDB_VALUE_TYPE_STRING,
        WDB_VALUE_TYPE_BOOL,
        WDB_VALUE_TYPE_INT_ARRAY,
        WDB_VALUE_TYPE_UINT_ARRAY,
        WDB_VALUE_TYPE_STRING_ARRAY,
        WDB_VALUE_TYPE_UNKNOWN
    }

    // Internal C# representation of the C WDBValue struct
    [StructLayout(LayoutKind.Sequential)]
    internal struct WDBValueInternal
    {
        public WDBValueType type;
        public WDBValueData data;
    }

    // Internal C# representation of array structs within C WDBValue union
    [StructLayout(LayoutKind.Sequential)]
    internal struct WDBIntArrayInternal
    {
        public IntPtr items; // int*
        public int count;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WDBUIntArrayInternal
    {
        public IntPtr items; // unsigned int*
        public int count;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WDBStringArrayInternal
    {
        public IntPtr items; // char**
        public int count;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct WDBValueData
    {
        [FieldOffset(0)] public int int_val;
        [FieldOffset(0)] public uint uint_val;
        [FieldOffset(0)] public float float_val;
        [FieldOffset(0)] public IntPtr string_val; // char*
        [FieldOffset(0)] public int bool_val; // C doesn't have a direct bool type, using int

        // These occupy the same memory location for a C union
        [FieldOffset(0)] public WDBIntArrayInternal int_array_val;
        [FieldOffset(0)] public WDBUIntArrayInternal uint_array_val;
        [FieldOffset(0)] public WDBStringArrayInternal string_array_val;
    }

    // Internal C# representation of the C WDBEntry struct
    [StructLayout(LayoutKind.Sequential)]
    internal struct WDBEntryInternal
    {
        public IntPtr key;   // char*
        public WDBValueInternal value;
    }

    // Internal C# representation of the C WDBSectionC struct
    [StructLayout(LayoutKind.Sequential)]
    internal struct WDBSectionCInternal
    {
        public IntPtr entries; // WDBEntry*
        public int entryCount;
    }

    // Internal C# representation of the C WDBRecordC struct
    [StructLayout(LayoutKind.Sequential)]
    internal struct WDBRecordCInternal
    {
        public IntPtr entries; // WDBEntry*
        public int entryCount;
    }

    // Internal C# representation of the C WDBFileC struct
    [StructLayout(LayoutKind.Sequential)]
    internal struct WDBFileCInternal
    {
        public IntPtr wdbName; // char*
        public WDBSectionCInternal header;
        public IntPtr records; // WDBRecordC*
        public int recordCount;
    }


    internal static class NativeMemoryManager
    {
        // Helper to allocate and copy a C# string to unmanaged memory
        public static IntPtr AllocString(string s)
        {
            if (s == null) return IntPtr.Zero;
            IntPtr ptr = Marshal.StringToHGlobalAnsi(s); // Use Ansi for char*
            return ptr;
        }

        // Helper to free a string allocated by AllocString
        public static void FreeString(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        // Helper to allocate and copy an array of ints
        public static WDBIntArrayInternal AllocIntArray(List<int> list)
        {
            WDBIntArrayInternal intArray = new WDBIntArrayInternal();
            intArray.count = list?.Count ?? 0;
            if (intArray.count > 0)
            {
                int[] arr = list.ToArray();
                intArray.items = Marshal.AllocHGlobal(intArray.count * sizeof(int));
                Marshal.Copy(arr, 0, intArray.items, intArray.count);
            }
            else
            {
                intArray.items = IntPtr.Zero;
            }
            return intArray;
        }

        // Helper to free an array of ints
        public static void FreeIntArray(ref WDBIntArrayInternal intArray)
        {
            if (intArray.items != IntPtr.Zero) Marshal.FreeHGlobal(intArray.items);
            intArray.items = IntPtr.Zero;
            intArray.count = 0;
        }
        
        // Helper to allocate and copy an array of uints
        public static WDBUIntArrayInternal AllocUIntArray(List<uint> list)
        {
            WDBUIntArrayInternal uintArray = new WDBUIntArrayInternal();
            uintArray.count = list?.Count ?? 0;
            if (uintArray.count > 0)
            {
                uint[] arr = list.ToArray();
                uintArray.items = Marshal.AllocHGlobal(uintArray.count * sizeof(uint));
                Marshal.Copy(MemoryMarshal.Cast<uint, byte>(arr).ToArray(), 0, uintArray.items, uintArray.count * sizeof(uint)); // Manual copy for uint
            }
            else
            {
                uintArray.items = IntPtr.Zero;
            }
            return uintArray;
        }

        // Helper to free an array of uints
        public static void FreeUIntArray(ref WDBUIntArrayInternal uintArray)
        {
            if (uintArray.items != IntPtr.Zero) Marshal.FreeHGlobal(uintArray.items);
            uintArray.items = IntPtr.Zero;
            uintArray.count = 0;
        }

        // Helper to allocate and copy an array of strings (char**)
        public static WDBStringArrayInternal AllocStringArray(List<string> list)
        {
            WDBStringArrayInternal stringArray = new WDBStringArrayInternal();
            stringArray.count = list?.Count ?? 0;
            if (stringArray.count > 0)
            {
                IntPtr[] ptrArray = new IntPtr[stringArray.count];
                for (int i = 0; i < stringArray.count; i++)
                {
                    ptrArray[i] = AllocString(list[i]);
                }

                stringArray.items = Marshal.AllocHGlobal(stringArray.count * IntPtr.Size);
                Marshal.Copy(ptrArray, 0, stringArray.items, stringArray.count);
            }
            else
            {
                stringArray.items = IntPtr.Zero;
            }
            return stringArray;
        }

        // Helper to free an array of strings (char**)
        public static void FreeStringArray(ref WDBStringArrayInternal stringArray)
        {
            if (stringArray.items != IntPtr.Zero && stringArray.count > 0)
            {
                IntPtr[] ptrArray = new IntPtr[stringArray.count];
                Marshal.Copy(stringArray.items, ptrArray, 0, stringArray.count);

                for (int i = 0; i < stringArray.count; i++)
                {
                    FreeString(ptrArray[i]);
                }
                Marshal.FreeHGlobal(stringArray.items);
            }
            stringArray.items = IntPtr.Zero;
            stringArray.count = 0;
        }

        // Main function to free all memory associated with a WDBFileCInternal
        public static void FreeWDBFileCInternal(ref WDBFileCInternal wdbFileC)
        {
            FreeString(wdbFileC.wdbName);

            // Free header section
            FreeWDBSectionCInternal(ref wdbFileC.header);

            // Free records
            if (wdbFileC.records != IntPtr.Zero && wdbFileC.recordCount > 0)
            {
                for (int i = 0; i < wdbFileC.recordCount; i++)
                {
                    IntPtr recordPtr = IntPtr.Add(wdbFileC.records, i * Marshal.SizeOf<WDBRecordCInternal>());
                    WDBRecordCInternal record = Marshal.PtrToStructure<WDBRecordCInternal>(recordPtr);
                    FreeWDBRecordCInternal(ref record);
                }
                Marshal.FreeHGlobal(wdbFileC.records);
            }

            wdbFileC.wdbName = IntPtr.Zero;
            wdbFileC.header = new WDBSectionCInternal(); // Zero out
            wdbFileC.records = IntPtr.Zero;
            wdbFileC.recordCount = 0;
        }

        // Helper to free a WDBSectionCInternal
        public static void FreeWDBSectionCInternal(ref WDBSectionCInternal sectionC)
        {
            if (sectionC.entries != IntPtr.Zero && sectionC.entryCount > 0)
            {
                for (int i = 0; i < sectionC.entryCount; i++)
                {
                    IntPtr entryPtr = IntPtr.Add(sectionC.entries, i * Marshal.SizeOf<WDBEntryInternal>());
                    WDBEntryInternal entry = Marshal.PtrToStructure<WDBEntryInternal>(entryPtr);
                    FreeWDBEntryInternal(ref entry);
                }
                Marshal.FreeHGlobal(sectionC.entries);
            }
            sectionC.entries = IntPtr.Zero;
            sectionC.entryCount = 0;
        }

        // Helper to free a WDBRecordCInternal (same as section but semantically different)
        public static void FreeWDBRecordCInternal(ref WDBRecordCInternal recordC)
        {
            if (recordC.entries != IntPtr.Zero && recordC.entryCount > 0)
            {
                for (int i = 0; i < recordC.entryCount; i++)
                {
                    IntPtr entryPtr = IntPtr.Add(recordC.entries, i * Marshal.SizeOf<WDBEntryInternal>());
                    WDBEntryInternal entry = Marshal.PtrToStructure<WDBEntryInternal>(entryPtr);
                    FreeWDBEntryInternal(ref entry);
                }
                Marshal.FreeHGlobal(recordC.entries);
            }
            recordC.entries = IntPtr.Zero;
            recordC.entryCount = 0;
        }

        // Helper to free a WDBEntryInternal
        public static void FreeWDBEntryInternal(ref WDBEntryInternal entryC)
        {
            FreeString(entryC.key);
            FreeWDBValueInternal(ref entryC.value);
            entryC.key = IntPtr.Zero;
            entryC.value = new WDBValueInternal(); // Zero out
        }

        // Helper to free a WDBValueInternal based on its type
        public static void FreeWDBValueInternal(ref WDBValueInternal valueC)
        {
            switch (valueC.type)
            {
                case WDBValueType.WDB_VALUE_TYPE_STRING:
                    FreeString(valueC.data.string_val);
                    break;
                case WDBValueType.WDB_VALUE_TYPE_INT_ARRAY:
                    WDBIntArrayInternal intArray = valueC.data.int_array_val;
                    FreeIntArray(ref intArray);
                    valueC.data.int_array_val = intArray; // Update the union member
                    break;
                case WDBValueType.WDB_VALUE_TYPE_UINT_ARRAY:
                    WDBUIntArrayInternal uintArray = valueC.data.uint_array_val;
                    FreeUIntArray(ref uintArray);
                    valueC.data.uint_array_val = uintArray; // Update the union member
                    break;
                case WDBValueType.WDB_VALUE_TYPE_STRING_ARRAY:
                    WDBStringArrayInternal stringArray = valueC.data.string_array_val;
                    FreeStringArray(ref stringArray);
                    valueC.data.string_array_val = stringArray; // Update the union member
                    break;
                // No freeing needed for primitive types
            }
            // Zero out data
            valueC.data = new WDBValueData();
            valueC.type = WDBValueType.WDB_VALUE_TYPE_UNKNOWN;
        }
    }


    internal static class NativeApi
    {
        [UnmanagedCallersOnly(EntryPoint = "WDB_Initialize")]
        public static void WDB_Initialize()
        {
            // Perform any necessary one-time initialization here
            // For now, it's just a placeholder
            Log.Info("WDB_Initialize called.");
        }

        [UnmanagedCallersOnly(EntryPoint = "WDB_ParseFile")]
        public static int WDB_ParseFile(IntPtr filePathPtr, IntPtr gameCodePtr, IntPtr outWDBFilePtr)
        {
            if (filePathPtr == IntPtr.Zero || gameCodePtr == IntPtr.Zero || outWDBFilePtr == IntPtr.Zero)
            {
                Log.Error("WDB_ParseFile received null pointer arguments.");
                return -1; // Indicate failure
            }

            string filePath = Marshal.PtrToStringAnsi(filePathPtr);
            string gameCode = Marshal.PtrToStringAnsi(gameCodePtr);

            Log.Info($"WDB_ParseFile called for file: {filePath} with gameCode: {gameCode}");

            try
            {
                WDBFile wdbFile;
                bool shouldIgnoreKnown = false; // Default or determined by another argument if needed

                if (gameCode.Equals("ff131", StringComparison.OrdinalIgnoreCase))
                {
                    wdbFile = XIII.Extraction.ExtractionMain.StartExtraction(filePath, shouldIgnoreKnown);
                }
                else if (gameCode.Equals("ff132", StringComparison.OrdinalIgnoreCase))
                {
                    wdbFile = XIII2LR.Extraction.ExtractionMain.StartExtraction(filePath);
                }
                else
                {
                    Log.Error($"Unsupported game code: {gameCode}");
                    return -1;
                }
                
                // Marshal WDBFile to WDBFileCInternal
                // The wdbFile.Sections dictionary always contains a single "header" WDBSection
                // The wdbFile.Records is a List<WDBRecord>
                WDBFileCInternal wdbFileC = MarshalWDBFileToC(wdbFile, wdbFile.Sections[JsonVariables.HeaderSectionToken], wdbFile.Records);

                // Copy the WDBFileCInternal struct to the unmanaged memory provided by C
                Marshal.StructureToPtr(wdbFileC, outWDBFilePtr, false);

                Log.Info($"Successfully parsed file: {filePath}");
                return 0; // Indicate success
            }
            catch (Exception ex)
            {
                Log.Error($"Error parsing file {filePath}: {ex.Message}");
                // Optionally, marshal error message back to C if WDBFileC includes an error field
                return -1; // Indicate failure
            }
        }

        // Helper to marshal WDBFile (C# object) to WDBFileCInternal (C-compatible struct)
        private static WDBFileCInternal MarshalWDBFileToC(WDBFile wdbFile, WDBSection header, List<WDBRecord> records)
        {
            WDBFileCInternal wdbFileC = new WDBFileCInternal();
            wdbFileC.wdbName = NativeMemoryManager.AllocString(wdbFile.WDBName);

            // Marshal header section
            wdbFileC.header = MarshalWDBSectionToC(header);

            // Marshal records
            wdbFileC.recordCount = records?.Count ?? 0;
            if (wdbFileC.recordCount > 0)
            {
                wdbFileC.records = Marshal.AllocHGlobal(wdbFileC.recordCount * Marshal.SizeOf<WDBRecordCInternal>());
                for (int i = 0; i < wdbFileC.recordCount; i++)
                {
                    WDBRecordCInternal recordC = MarshalWDBRecordToC(records[i]);
                    IntPtr recordPtr = IntPtr.Add(wdbFileC.records, i * Marshal.SizeOf<WDBRecordCInternal>());
                    Marshal.StructureToPtr(recordC, recordPtr, false);
                }
            }
            else
            {
                wdbFileC.records = IntPtr.Zero;
            }

            return wdbFileC;
        }

        private static WDBSectionCInternal MarshalWDBSectionToC(WDBSection section)
        {
            WDBSectionCInternal sectionC = new WDBSectionCInternal();
            sectionC.entryCount = section?.Count ?? 0;
            if (sectionC.entryCount > 0)
            {
                sectionC.entries = Marshal.AllocHGlobal(sectionC.entryCount * Marshal.SizeOf<WDBEntryInternal>());
                int i = 0;
                foreach (var entry in section)
                {
                    WDBEntryInternal entryC = MarshalWDBEntryToC(entry.Key, entry.Value);
                    IntPtr entryPtr = IntPtr.Add(sectionC.entries, i * Marshal.SizeOf<WDBEntryInternal>());
                    Marshal.StructureToPtr(entryC, entryPtr, false);
                    i++;
                }
            }
            else
            {
                sectionC.entries = IntPtr.Zero;
            }
            return sectionC;
        }

        private static WDBRecordCInternal MarshalWDBRecordToC(WDBRecord record)
        {
            WDBRecordCInternal recordC = new WDBRecordCInternal();
            recordC.entryCount = record?.Count ?? 0;
            if (recordC.entryCount > 0)
            {
                recordC.entries = Marshal.AllocHGlobal(recordC.entryCount * Marshal.SizeOf<WDBEntryInternal>());
                int i = 0;
                foreach (var entry in record)
                {
                    WDBEntryInternal entryC = MarshalWDBEntryToC(entry.Key, entry.Value);
                    IntPtr entryPtr = IntPtr.Add(recordC.entries, i * Marshal.SizeOf<WDBEntryInternal>());
                    Marshal.StructureToPtr(entryC, entryPtr, false);
                    i++;
                }
            }
            else
            {
                recordC.entries = IntPtr.Zero;
            }
            return recordC;
        }

        private static WDBEntryInternal MarshalWDBEntryToC(string key, object value)
        {
            WDBEntryInternal entryC = new WDBEntryInternal();
            entryC.key = NativeMemoryManager.AllocString(key);
            entryC.value = MarshalWDBValueToC(value);
            return entryC;
        }

        private static WDBValueInternal MarshalWDBValueToC(object value)
        {
            WDBValueInternal valueC = new WDBValueInternal();

            switch (value)
            {
                case int i:
                    valueC.type = WDBValueType.WDB_VALUE_TYPE_INT;
                    valueC.data.int_val = i;
                    break;
                case uint u:
                    valueC.type = WDBValueType.WDB_VALUE_TYPE_UINT;
                    valueC.data.uint_val = u;
                    break;
                case float f:
                    valueC.type = WDBValueType.WDB_VALUE_TYPE_FLOAT;
                    valueC.data.float_val = f;
                    break;
                case string s:
                    valueC.type = WDBValueType.WDB_VALUE_TYPE_STRING;
                    valueC.data.string_val = NativeMemoryManager.AllocString(s);
                    break;
                case bool b:
                    valueC.type = WDBValueType.WDB_VALUE_TYPE_BOOL;
                    valueC.data.bool_val = b ? 1 : 0;
                    break;
                case List<int> intList:
                    valueC.type = WDBValueType.WDB_VALUE_TYPE_INT_ARRAY;
                    valueC.data.int_array_val = NativeMemoryManager.AllocIntArray(intList);
                    break;
                case List<uint> uintList:
                    valueC.type = WDBValueType.WDB_VALUE_TYPE_UINT_ARRAY;
                    valueC.data.uint_array_val = NativeMemoryManager.AllocUIntArray(uintList);
                    break;
                case List<string> stringList:
                    valueC.type = WDBValueType.WDB_VALUE_TYPE_STRING_ARRAY;
                    valueC.data.string_array_val = NativeMemoryManager.AllocStringArray(stringList);
                    break;
                default:
                    Log.Warn($"Unsupported type encountered during marshaling: {value?.GetType().Name ?? "null"}");
                    valueC.type = WDBValueType.WDB_VALUE_TYPE_UNKNOWN;
                    break;
            }
            return valueC;
        }


        [UnmanagedCallersOnly(EntryPoint = "WDB_FreeWDBFile")]
        public static void WDB_FreeWDBFile(IntPtr wdbFilePtr)
        {
            if (wdbFilePtr == IntPtr.Zero)
            {
                Log.Warn("WDB_FreeWDBFile called with null pointer.");
                return;
            }

            WDBFileCInternal wdbFileC = Marshal.PtrToStructure<WDBFileCInternal>(wdbFilePtr);
            NativeMemoryManager.FreeWDBFileCInternal(ref wdbFileC);
            Log.Info("WDB_FreeWDBFile completed.");
        }

        [UnmanagedCallersOnly(EntryPoint = "WDB_FreeString")]
        public static void WDB_FreeString(IntPtr strPtr)
        {
            if (strPtr == IntPtr.Zero)
            {
                Log.Warn("WDB_FreeString called with null pointer.");
                return;
            }
            NativeMemoryManager.FreeString(strPtr);
            Log.Info("WDB_FreeString completed.");
        }
    }
}
