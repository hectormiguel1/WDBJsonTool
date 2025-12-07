using System.Runtime.InteropServices;
using WDBJsonTool.Native;

namespace WDBJsonTool
{
    public static unsafe class NativeExports
    {
        #region JSON Export Functions (existing functionality)

        /// <summary>
        /// Extract WDB to JSON for FFXIII (ff131)
        /// </summary>
        /// <param name="wdbFilePath">Path to the .wdb file (null-terminated UTF-8 string)</param>
        /// <param name="ignoreKnown">If true, ignores known field names</param>
        /// <returns>0 on success, non-zero on error</returns>
        [UnmanagedCallersOnly(EntryPoint = "wdb_extract_json_xiii", CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static int ExtractJsonXIII(IntPtr wdbFilePath, byte ignoreKnown)
        {
            try
            {
                var path = Marshal.PtrToStringUTF8(wdbFilePath);
                if (string.IsNullOrEmpty(path))
                    return -1;

                XIII.Extraction.ExtractionMain.StartExtraction(path, ignoreKnown != 0);
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Convert JSON to WDB for FFXIII (ff131)
        /// </summary>
        /// <param name="jsonFilePath">Path to the .json file (null-terminated UTF-8 string)</param>
        /// <returns>0 on success, non-zero on error</returns>
        [UnmanagedCallersOnly(EntryPoint = "wdb_convert_json_xiii", CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static int ConvertJsonXIII(IntPtr jsonFilePath)
        {
            try
            {
                var path = Marshal.PtrToStringUTF8(jsonFilePath);
                if (string.IsNullOrEmpty(path))
                    return -1;

                XIII.Conversion.ConversionMain.StartConversion(path);
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Extract WDB to JSON for FFXIII-2 and Lightning Returns (ff132)
        /// </summary>
        /// <param name="wdbFilePath">Path to the .wdb file (null-terminated UTF-8 string)</param>
        /// <returns>0 on success, non-zero on error</returns>
        [UnmanagedCallersOnly(EntryPoint = "wdb_extract_json_xiii2lr", CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static int ExtractJsonXIII2LR(IntPtr wdbFilePath)
        {
            try
            {
                var path = Marshal.PtrToStringUTF8(wdbFilePath);
                if (string.IsNullOrEmpty(path))
                    return -1;

                XIII2LR.Extraction.ExtractionMain.StartExtraction(path);
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Convert JSON to WDB for FFXIII-2 and Lightning Returns (ff132)
        /// </summary>
        /// <param name="jsonFilePath">Path to the .json file (null-terminated UTF-8 string)</param>
        /// <returns>0 on success, non-zero on error</returns>
        [UnmanagedCallersOnly(EntryPoint = "wdb_convert_json_xiii2lr", CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static int ConvertJsonXIII2LR(IntPtr jsonFilePath)
        {
            try
            {
                var path = Marshal.PtrToStringUTF8(jsonFilePath);
                if (string.IsNullOrEmpty(path))
                    return -1;

                XIII2LR.Conversion.ConversionMain.StartConversion(path);
                return 0;
            }
            catch
            {
                return -1;
            }
        }

        #endregion

        #region Native Struct Functions

        /// <summary>
        /// Parse WDB file and return native struct for FFXIII
        /// </summary>
        /// <param name="wdbFilePath">Path to the .wdb file (null-terminated UTF-8 string)</param>
        /// <param name="ignoreKnown">If true, ignores known field names</param>
        /// <returns>Pointer to WdbFile struct, or null on error. Caller must free with wdb_free_xiii()</returns>
        [UnmanagedCallersOnly(EntryPoint = "wdb_parse_xiii", CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static WdbFile* ParseXIII(IntPtr wdbFilePath, byte ignoreKnown)
        {
            try
            {
                var path = Marshal.PtrToStringUTF8(wdbFilePath);
                if (string.IsNullOrEmpty(path))
                    return null;

                return NativeParser.ParseXIII(path, ignoreKnown != 0);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parse WDB file and return native struct for FFXIII-2/LR
        /// </summary>
        /// <param name="wdbFilePath">Path to the .wdb file (null-terminated UTF-8 string)</param>
        /// <returns>Pointer to WdbFileXIII2LR struct, or null on error. Caller must free with wdb_free_xiii2lr()</returns>
        [UnmanagedCallersOnly(EntryPoint = "wdb_parse_xiii2lr", CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static WdbFileXIII2LR* ParseXIII2LR(IntPtr wdbFilePath)
        {
            try
            {
                var path = Marshal.PtrToStringUTF8(wdbFilePath);
                if (string.IsNullOrEmpty(path))
                    return null;

                return NativeParser.ParseXIII2LR(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Free a WdbFile structure allocated by wdb_parse_xiii
        /// </summary>
        /// <param name="wdbFile">Pointer to WdbFile to free</param>
        [UnmanagedCallersOnly(EntryPoint = "wdb_free_xiii", CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static void FreeXIII(WdbFile* wdbFile)
        {
            NativeMemoryManager.FreeWdbFile(wdbFile);
        }

        /// <summary>
        /// Free a WdbFileXIII2LR structure allocated by wdb_parse_xiii2lr
        /// </summary>
        /// <param name="wdbFile">Pointer to WdbFileXIII2LR to free</param>
        [UnmanagedCallersOnly(EntryPoint = "wdb_free_xiii2lr", CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static void FreeXIII2LR(WdbFileXIII2LR* wdbFile)
        {
            NativeMemoryManager.FreeWdbFileXIII2LR(wdbFile);
        }

        #endregion
    }
}

