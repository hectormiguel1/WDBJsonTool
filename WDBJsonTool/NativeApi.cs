using System.Runtime.InteropServices;
using Native.Common;
using WDBJsonTool;
using WDBJsonTool.DataStructures;
using WDBJsonTool.Native;
using WDBJsonTool.Support; // For Log.Info


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
    internal unsafe struct WDBIntArrayInternal
    {
        public int* items; // int*
        public int count;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct WDBUIntArrayInternal
    {
        public int* items; // unsigned int*
        public int count;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct WDBStringArrayInternal
    {
        public byte** items; // char**
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
    internal unsafe struct WDBEntryInternal
    {
        public byte* key;   // char*
        public WDBValueInternal value;
    }

    // Internal C# representation of the C WDBSectionC struct
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct WDBSectionCInternal
    {
        public WDBEntryInternal* entries; // WDBEntry*
        public int entryCount;
    }

    // Internal C# representation of the C WDBRecordC struct
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct WDBRecordCInternal
    {
        public WDBEntryInternal* entries; // WDBEntry*
        public int entryCount;
    }

    // Internal C# representation of the C WDBFileC struct
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct WDBFileCInternal
    {
        public IntPtr wdbName; // char*
        public WDBSectionCInternal header;
        public WDBRecordCInternal* records; // WDBRecordC*
        public int recordCount;
    }

    public enum GameCode : int
    {
        ff13 = 0,
        ff132 = 1
    }

    internal static class NativeMemoryManager
    {
        // Helper to allocate and copy a C# string to unmanaged memory
        public static IntPtr AllocString(string s)
        {
            var ptr = Marshal.StringToHGlobalAnsi(s); // Use Ansi for char*
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
        public static unsafe WDBIntArrayInternal AllocIntArray(List<int> list)
        {
            var intArray = new WDBIntArrayInternal
            {
                count = list?.Count ?? 0
            };
            if (intArray.count > 0)
            {
                var arr = list?.ToArray();
                intArray.items = (int*)Marshal.AllocHGlobal(intArray.count * sizeof(int));
                if (arr != null) Marshal.Copy(arr, 0, (IntPtr)intArray.items, intArray.count);
            }
            else
            {
                intArray.items = null;
            }
            return intArray;
        }

        // Helper to free an array of ints
        private static unsafe void FreeIntArray(ref WDBIntArrayInternal intArray)
        {
            if (intArray.items != null) Marshal.FreeHGlobal((IntPtr)intArray.items);
            intArray.items = null;
            intArray.count = 0;
        }
        
        // Helper to allocate and copy an array of uints
        public static unsafe WDBUIntArrayInternal AllocUIntArray(List<uint> list)
        {
            var uintArray = new WDBUIntArrayInternal();
            uintArray.count = list?.Count ?? 0;
            if (uintArray.count > 0)
            {
                var arr = list.ToArray();
                uintArray.items = (int*)Marshal.AllocHGlobal(uintArray.count * sizeof(uint)); // Use int* for uint* storage
                // Marshal.Copy for uint array requires unsafe pointer copy or casting
                fixed (uint* pSrc = arr)
                {
                    var bytes = uintArray.count * sizeof(uint);
                    Buffer.MemoryCopy(pSrc, uintArray.items, bytes, bytes);
                }
            }
            else
            {
                uintArray.items = null;
            }
            return uintArray;
        }

        // Helper to free an array of uints
        private static unsafe void FreeUIntArray(ref WDBUIntArrayInternal uintArray)
        {
            if (uintArray.items != null) Marshal.FreeHGlobal((IntPtr)uintArray.items);
            uintArray.items = null;
            uintArray.count = 0;
        }

        // Helper to allocate and copy an array of strings (char**)
        public static unsafe WDBStringArrayInternal AllocStringArray(List<string> list)
        {
            var stringArray = new WDBStringArrayInternal
            {
                count = list?.Count ?? 0
            };
            if (stringArray.count > 0)
            {
                var ptrArray = new IntPtr[stringArray.count];
                for (var i = 0; i < stringArray.count; i++)
                {
                    ptrArray[i] = AllocString(list[i]);
                }

                stringArray.items = (byte**)Marshal.AllocHGlobal(stringArray.count * IntPtr.Size);
                Marshal.Copy(ptrArray, 0, (IntPtr)stringArray.items, stringArray.count);
            }
            else
            {
                stringArray.items = null;
            }
            return stringArray;
        }

        // Helper to free an array of strings (char**)
        private static unsafe void FreeStringArray(ref WDBStringArrayInternal stringArray)
        {
            if (stringArray.items != null && stringArray.count > 0)
            {
                var ptrArray = new IntPtr[stringArray.count];
                Marshal.Copy((IntPtr)stringArray.items, ptrArray, 0, stringArray.count);

                for (var i = 0; i < stringArray.count; i++)
                {
                    FreeString(ptrArray[i]);
                }
                Marshal.FreeHGlobal((IntPtr)stringArray.items);
            }
            stringArray.items = null;
            stringArray.count = 0;
        }

        // Main function to free all memory associated with a WDBFileCInternal
        public static unsafe void FreeWDBFileCInternal(ref WDBFileCInternal wdbFileC)
        {
            FreeString(wdbFileC.wdbName);

            // Free header section
            FreeWDBSectionCInternal(ref wdbFileC.header);

            // Free records
            if (wdbFileC.records != null && wdbFileC.recordCount > 0)
            {
                for (var i = 0; i < wdbFileC.recordCount; i++)
                {
                    var recordPtr = (IntPtr)(wdbFileC.records + i);
                    var record = Marshal.PtrToStructure<WDBRecordCInternal>(recordPtr);
                    FreeWDBRecordCInternal(ref record);
                }
                Marshal.FreeHGlobal((IntPtr)wdbFileC.records);
            }

            wdbFileC.wdbName = IntPtr.Zero;
            wdbFileC.header = new WDBSectionCInternal(); // Zero out
            wdbFileC.records = null;
            wdbFileC.recordCount = 0;
        }

        // Helper to free a WDBSectionCInternal
        private static unsafe void FreeWDBSectionCInternal(ref WDBSectionCInternal sectionC)
        {
            if (sectionC.entries != null && sectionC.entryCount > 0)
            {
                for (var i = 0; i < sectionC.entryCount; i++)
                {
                    var entryPtr = (IntPtr)(sectionC.entries + i);
                    var entry = Marshal.PtrToStructure<WDBEntryInternal>(entryPtr);
                    FreeWDBEntryInternal(ref entry);
                }
                Marshal.FreeHGlobal((IntPtr)sectionC.entries);
            }
            sectionC.entries = null;
            sectionC.entryCount = 0;
        }

        // Helper to free a WDBRecordCInternal (same as section but semantically different)
        private static unsafe void FreeWDBRecordCInternal(ref WDBRecordCInternal recordC)
        {
            if (recordC.entries != null && recordC.entryCount > 0)
            {
                for (var i = 0; i < recordC.entryCount; i++)
                {
                    var entryPtr = (IntPtr)(recordC.entries + i);
                    var entry = Marshal.PtrToStructure<WDBEntryInternal>(entryPtr);
                    FreeWDBEntryInternal(ref entry);
                }
                Marshal.FreeHGlobal((IntPtr)recordC.entries);
            }
            recordC.entries = null;
            recordC.entryCount = 0;
        }

        // Helper to free a WDBEntryInternal
        private static void FreeWDBEntryInternal(ref WDBEntryInternal entryC)
        {
            // Note: entryC.key is byte* (char*), needs cast to IntPtr for FreeString
            // FreeString checks for Zero.
            unsafe 
            {
                 NativeMemoryManager.FreeString((IntPtr)entryC.key);
                 entryC.key = null;
            }
            FreeWDBValueInternal(ref entryC.value);
            
            entryC.value = new WDBValueInternal(); // Zero out
        }

        // Helper to free a WDBValueInternal based on its type
        private static void FreeWDBValueInternal(ref WDBValueInternal valueC)
        {
            switch (valueC.type)
            {
                case WDBValueType.WDB_VALUE_TYPE_STRING:
                    FreeString(valueC.data.string_val);
                    break;
                case WDBValueType.WDB_VALUE_TYPE_INT_ARRAY:
                {
                    var intArray = valueC.data.int_array_val;
                    FreeIntArray(ref intArray);
                    valueC.data.int_array_val = intArray; // Update the union member
                    break;
                }
                case WDBValueType.WDB_VALUE_TYPE_UINT_ARRAY:
                {
                    var uintArray = valueC.data.uint_array_val;
                    FreeUIntArray(ref uintArray);
                    valueC.data.uint_array_val = uintArray; // Update the union member
                    break;
                }
                case WDBValueType.WDB_VALUE_TYPE_STRING_ARRAY:
                {
                    var stringArray = valueC.data.string_array_val;
                    FreeStringArray(ref stringArray);
                    valueC.data.string_array_val = stringArray; // Update the union member
                    break;
                }
            }

            // No freeing needed for primitive types
            // Zero out data
            valueC.data = new WDBValueData();
            valueC.type = WDBValueType.WDB_VALUE_TYPE_UNKNOWN;
        }
    }


    internal static class NativeApi
    {

        [UnmanagedCallersOnly(EntryPoint = "WDB_ParseFile")]
        public static unsafe NativeResult.Result<WDBFileCInternal> WDB_ParseFile(byte* filePathPtr, byte gameCodeRaw)
        {
            
            var filePath = NativeResult.StringFromPtr(filePathPtr);
            var gameCode = (GameCode) gameCodeRaw;
            if (string.IsNullOrEmpty(filePath) )
            {
                const string msg = "WDB_ParseFile received null pointer arguments.";
                Log.Fatal(msg);
                return NativeResult.CreateError<WDBFileCInternal>(msg, -1); // Indicate failure
            }
            
            Log.Info($"WDB_ParseFile called for file: {filePath} with gameCode: {gameCode}");

            try
            {
                
                const bool shouldIgnoreKnown = false; // Default or determined by another argument if needed

                var wdbFile = gameCode switch
                {
                    GameCode.ff13 => WDBJsonTool.XIII.Extraction.ExtractionMain.StartExtraction(filePath, shouldIgnoreKnown),
                    GameCode.ff132 => WDBJsonTool.XIII2LR.Extraction.ExtractionMain.StartExtraction(filePath),
                    _ => null
                };
                if (wdbFile == null)
                {
                    var msg = $"Invalid Game code: {gameCode}!";
                    Log.Fatal(msg);
                    return NativeResult.CreateError<WDBFileCInternal>(msg, -1);
                }
                
                // Marshal WDBFile to WDBFileCInternal
                // The wdbFile.Sections dictionary always contains a single "header" WDBSection
                // The wdbFile.Records is a List<WDBRecord>
                var wdbFileC = MarshalWDBFileToC(wdbFile, wdbFile.Sections[JsonVariables.HeaderSectionToken], wdbFile.Records);

                Log.Info($"Successfully parsed file: {filePath}");
                return NativeResult.CreateSuccess<WDBFileCInternal>(wdbFileC); // Indicate success
            }
            catch (Exception ex)
            {
                Log.Fatal($"Error parsing file {filePath}: {ex.Message}");
                // Optionally, marshal error message back to C if WDBFileC includes an error field
                return NativeResult.CreateError<WDBFileCInternal>(ex.Message, -1); // Indicate failure
            }
        }

        // Helper to marshal WDBFile (C# object) to WDBFileCInternal (C-compatible struct)
        private static unsafe WDBFileCInternal MarshalWDBFileToC(WDBFile wdbFile, WDBSection header, List<WDBRecord> records)
        {
            var wdbFileC = new WDBFileCInternal();
            wdbFileC.wdbName = NativeMemoryManager.AllocString(wdbFile.WDBName);

            // Marshal header section
            wdbFileC.header = MarshalWDBSectionToC(header);

            // Marshal records
            wdbFileC.recordCount = records?.Count ?? 0;
            if (wdbFileC.recordCount > 0)
            {
                wdbFileC.records = (WDBRecordCInternal*)Marshal.AllocHGlobal(wdbFileC.recordCount * sizeof(WDBRecordCInternal));
                for (var i = 0; i < wdbFileC.recordCount; i++)
                {
                    var recordC = MarshalWDBRecordToC(records[i]);
                    var recordPtr = (IntPtr)(wdbFileC.records + i);
                    Marshal.StructureToPtr(recordC, recordPtr, false);
                }
            }
            else
            {
                wdbFileC.records = null;
            }

            return wdbFileC;
        }

        private static unsafe WDBSectionCInternal MarshalWDBSectionToC(WDBSection section)
        {
            var sectionC = new WDBSectionCInternal();
            sectionC.entryCount = section?.Count ?? 0;
            if (sectionC.entryCount > 0)
            {
                sectionC.entries = (WDBEntryInternal*)Marshal.AllocHGlobal(sectionC.entryCount * sizeof(WDBEntryInternal));
                var i = 0;
                foreach (var entry in section)
                {
                    var entryC = MarshalWDBEntryToC(entry.Key, entry.Value);
                    var entryPtr = (IntPtr)(sectionC.entries + i);
                    Marshal.StructureToPtr(entryC, entryPtr, false);
                    i++;
                }
            }
            else
            {
                sectionC.entries = null;
            }
            return sectionC;
        }

        private static unsafe WDBRecordCInternal MarshalWDBRecordToC(WDBRecord record)
        {
            var recordC = new WDBRecordCInternal();
            recordC.entryCount = record?.Count ?? 0;
            if (recordC.entryCount > 0)
            {
                recordC.entries = (WDBEntryInternal*)Marshal.AllocHGlobal(recordC.entryCount * sizeof(WDBEntryInternal));
                var i = 0;
                foreach (var entry in record)
                {
                    var entryC = MarshalWDBEntryToC(entry.Key, entry.Value);
                    var entryPtr = (IntPtr)(recordC.entries + i);
                    Marshal.StructureToPtr(entryC, entryPtr, false);
                    i++;
                }
            }
            else
            {
                recordC.entries = null;
            }
            return recordC;
        }

        private static unsafe WDBEntryInternal MarshalWDBEntryToC(string key, object value)
        {
            var entryC = new WDBEntryInternal();
            // AllocString returns IntPtr, key is byte*
            entryC.key = (byte*)NativeMemoryManager.AllocString(key);
            entryC.value = MarshalWDBValueToC(value);
            return entryC;
        }

        private static unsafe WDBValueInternal MarshalWDBValueToC(object value)
        {
            var valueC = new WDBValueInternal();

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
                    Log.Warning($"Unsupported type encountered during marshaling: {value?.GetType().Name ?? "null"}");
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
                Log.Warning("WDB_FreeWDBFile called with null pointer.");
                return;
            }

            var wdbFileC = Marshal.PtrToStructure<WDBFileCInternal>(wdbFilePtr);
            NativeMemoryManager.FreeWDBFileCInternal(ref wdbFileC);
            Log.Info("WDB_FreeWDBFile completed.");
        }

        [UnmanagedCallersOnly(EntryPoint = "WDB_WriteFile")]
        public static unsafe NativeResult.Result<int> WDB_WriteFile(byte* filePathPtr, byte gameCodeRaw, IntPtr inWDBFilePtr)
        {
            var filePath = NativeResult.StringFromPtr(filePathPtr);
            var gameCode = (GameCode)gameCodeRaw;

            if (string.IsNullOrEmpty(filePath) || inWDBFilePtr == IntPtr.Zero)
            {
                const string msg = "WDB_WriteFile received null pointer arguments.";
                Log.Fatal(msg);
                return NativeResult.CreateError<int>(msg, -1);
            }

            Log.Info($"WDB_WriteFile called for file: {filePath} with gameCode: {gameCode}");

            try
            {
                // Unmarshal WDBFileCInternal from C into C# WDBFile object
                var wdbFile = UnmarshalCFileToWDB(inWDBFilePtr);

                if (gameCode == GameCode.ff13)
                {
                    XIII.Conversion.WDBbuilder.BuildWDB(wdbFile, filePath);
                }
                else if (gameCode == GameCode.ff132)
                {
                    XIII2LR.Conversion.WDBbuilder.BuildWDB(wdbFile, filePath);
                }
                else
                {
                    var msg = $"Unsupported game code: {gameCode}";
                    Log.Fatal(msg);
                    return NativeResult.CreateError<int>(msg, -1);
                }

                Log.Info($"Successfully wrote file: {filePath}");
                return NativeResult.CreateSuccess<int>(0);
            }
            catch (Exception ex)
            {
                Log.Fatal($"Error writing file {filePath}: {ex.Message}");
                return NativeResult.CreateError<int>(ex.Message, -1);
            }
        }

        // Helper to unmarshal WDBFileCInternal (C-compatible struct) to WDBFile (C# object)
        private static unsafe WDBFile UnmarshalCFileToWDB(IntPtr inWDBFilePtr)
        {
            var wdbFileC = Marshal.PtrToStructure<WDBFileCInternal>(inWDBFilePtr);
            Log.Info($"UnmarshalCFileToWDB: wdbFileC.wdbName ptr: {wdbFileC.wdbName}");
            // Log.Info($"UnmarshalCFileToWDB: wdbFileC.records ptr: {wdbFileC.records}"); // Pointer format might fail default formatter?
            
            var wdbName = Marshal.PtrToStringAnsi(wdbFileC.wdbName);
            var header = UnmarshalCSectionToWDB(wdbFileC.header);

            List<WDBRecord> records = [];
            if (wdbFileC.records != null && wdbFileC.recordCount > 0)
            {
                for (var i = 0; i < wdbFileC.recordCount; i++)
                {
                    var recordPtr = (IntPtr)(wdbFileC.records + i);
                    var recordC = Marshal.PtrToStructure<WDBRecordCInternal>(recordPtr);
                    records.Add(UnmarshalCRecordToWDB(recordC));
                }
            }

            var wdbFile = new WDBFile
            {
                WDBName = wdbName,
                Sections = new Dictionary<string, WDBSection> { { JsonVariables.HeaderSectionToken, header } },
                Records = records
            };

            return wdbFile;
        }

        private static unsafe WDBSection UnmarshalCSectionToWDB(WDBSectionCInternal sectionC)
        {
            var section = new WDBSection();
            if (sectionC.entries != null && sectionC.entryCount > 0)
            {
                for (var i = 0; i < sectionC.entryCount; i++)
                {
                    var entryPtr = (IntPtr)(sectionC.entries + i);
                    var entryC = Marshal.PtrToStructure<WDBEntryInternal>(entryPtr);
                    (var key, var value) = UnmarshalCEntryToWDB(entryC);
                    section.Add(key, value);
                }
            }
            return section;
        }

        private static unsafe WDBRecord UnmarshalCRecordToWDB(WDBRecordCInternal recordC)
        {
            var record = new WDBRecord();
            if (recordC.entries != null && recordC.entryCount > 0)
            {
                for (var i = 0; i < recordC.entryCount; i++)
                {
                    var entryPtr = (IntPtr)(recordC.entries + i);
                    var entryC = Marshal.PtrToStructure<WDBEntryInternal>(entryPtr);
                    (var key, var value) = UnmarshalCEntryToWDB(entryC);
                    record.Add(key, value);
                }
            }
            return record;
        }

        private static unsafe (string key, object value) UnmarshalCEntryToWDB(WDBEntryInternal entryC)
        {
            var key = Marshal.PtrToStringAnsi((IntPtr)entryC.key);
            var value = UnmarshalCValueToWDB(entryC.value);
            return (key, value);
        }

        private static unsafe object UnmarshalCValueToWDB(WDBValueInternal valueC)
        {
            switch (valueC.type)
            {
                case WDBValueType.WDB_VALUE_TYPE_INT:
                    return valueC.data.int_val;
                case WDBValueType.WDB_VALUE_TYPE_UINT:
                    return valueC.data.uint_val;
                case WDBValueType.WDB_VALUE_TYPE_FLOAT:
                    return valueC.data.float_val;
                case WDBValueType.WDB_VALUE_TYPE_STRING:
                    return Marshal.PtrToStringAnsi(valueC.data.string_val);
                case WDBValueType.WDB_VALUE_TYPE_BOOL:
                    return valueC.data.bool_val != 0;
                case WDBValueType.WDB_VALUE_TYPE_INT_ARRAY:
                    return UnmarshalIntArray(valueC.data.int_array_val);
                case WDBValueType.WDB_VALUE_TYPE_UINT_ARRAY:
                    return UnmarshalUIntArray(valueC.data.uint_array_val);
                case WDBValueType.WDB_VALUE_TYPE_STRING_ARRAY:
                    return UnmarshalStringArray(valueC.data.string_array_val);
                default:
                    Log.Warning($"Unsupported WDBValueType encountered during unmarshaling: {valueC.type}");
                    return null;
            }
        }

        private static unsafe List<int> UnmarshalIntArray(WDBIntArrayInternal intArrayC)
        {
            List<int> list = [];
            if (intArrayC.items != null && intArrayC.count > 0)
            {
                var arr = new int[intArrayC.count];
                Marshal.Copy((IntPtr)intArrayC.items, arr, 0, intArrayC.count);
                list.AddRange(arr);
            }
            return list;
        }

        private static unsafe List<uint> UnmarshalUIntArray(WDBUIntArrayInternal uintArrayC)
        {
            List<uint> list = [];
            if (uintArrayC.items != null && uintArrayC.count > 0)
            {
                var arr = new uint[uintArrayC.count];
                // uint* items
                var pItems = (uint*)uintArrayC.items; 
                for (var i = 0; i < uintArrayC.count; i++)
                {
                    arr[i] = pItems[i];
                }
                list.AddRange(arr);
            }
            return list;
        }

        private static unsafe List<string> UnmarshalStringArray(WDBStringArrayInternal stringArrayC)
        {
            var list = new List<string>();
            if (stringArrayC.items == null || stringArrayC.count <= 0) return list;
            var ptrArray = new IntPtr[stringArrayC.count];
            Marshal.Copy((IntPtr)stringArrayC.items, ptrArray, 0, stringArrayC.count);

            for (var i = 0; i < stringArrayC.count; i++)
            {
                list.Add(Marshal.PtrToStringAnsi(ptrArray[i]));
            }
            return list;
        }


        [UnmanagedCallersOnly(EntryPoint = "WDB_FreeString")]
        public static void WDB_FreeString(IntPtr strPtr)
        {
            if (strPtr == IntPtr.Zero)
            {
                Log.Warning("WDB_FreeString called with null pointer.");
                return;
            }
            NativeMemoryManager.FreeString(strPtr);
            Log.Info("WDB_FreeString completed.");
        }
    }
}