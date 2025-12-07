using System.Runtime.InteropServices;
using WDBJsonTool.Support;

namespace WDBJsonTool.Native
{
    /// <summary>
    /// Parses WDB files and returns native structs
    /// </summary>
    public static unsafe class NativeParser
    {
        #region XIII Parser

        public static WdbFile* ParseXIII(string wdbFilePath, bool ignoreKnown)
        {
            var wdbVars = new XIII.WDBVariablesXIII
            {
                IgnoreKnown = ignoreKnown
            };

            using var wdbReader = new BinaryReader(File.Open(wdbFilePath, FileMode.Open, FileAccess.Read));

            wdbVars.WDBName = Path.GetFileNameWithoutExtension(wdbFilePath);

            wdbReader.BaseStream.Position = 0;
            if (wdbReader.ReadBytesString(3, false) != "WPD")
            {
                throw new InvalidDataException("Not a valid WPD file");
            }

            wdbReader.BaseStream.Position += 1;
            wdbVars.RecordCount = wdbReader.ReadBytesUInt32(true);

            if (wdbVars.RecordCount == 0)
            {
                throw new InvalidDataException("No records/sections are present in this file");
            }

            // Parse sections
            XIII.Extraction.SectionsParser.MainSections(wdbReader, wdbVars);

            // Allocate the WdbFile structure
            var wdbFile = (WdbFile*)Marshal.AllocHGlobal(sizeof(WdbFile));
            *wdbFile = new WdbFile();

            // Set sheet name
            NativeMemoryManager.CopyStringToFixedBuffer(wdbFile->SheetName, 64, wdbVars.SheetName);

            wdbFile->RecordCount = wdbVars.RecordCount;
            wdbFile->FieldDefinitionCount = wdbVars.FieldCount;
            wdbFile->IsKnown = (byte)(wdbVars.IsKnown ? 1 : 0);

            // Allocate and copy strtypelist values
            wdbFile->StrtypelistValues = NativeMemoryManager.AllocateUIntArray(wdbVars.StrtypelistValues);

            // Allocate field names if known
            if (wdbVars.IsKnown && wdbVars.Fields != null)
            {
                wdbFile->FieldNames = NativeMemoryManager.AllocateStringArray(wdbVars.Fields, out _);
            }

            // Build sections array
            BuildSectionsXIII(wdbFile, wdbVars);

            // Parse records
            ParseRecordsXIII(wdbReader, wdbVars, wdbFile);

            return wdbFile;
        }

        private static void BuildSectionsXIII(WdbFile* wdbFile, XIII.WDBVariablesXIII wdbVars)
        {
            var sectionList = new List<(string name, byte[]? data)>();

            if (wdbVars.StringsData != null)
                sectionList.Add((wdbVars.StringSectionName, wdbVars.StringsData));
            if (wdbVars.StrtypelistData != null)
                sectionList.Add((wdbVars.StrtypelistSectionName, wdbVars.StrtypelistData));
            if (wdbVars.TypelistData != null)
                sectionList.Add((wdbVars.TypelistSectionName, wdbVars.TypelistData));
            if (wdbVars.VersionData != null)
                sectionList.Add((wdbVars.VersionSectionName, wdbVars.VersionData));

            wdbFile->SectionCount = (uint)sectionList.Count;

            if (sectionList.Count > 0)
            {
                wdbFile->Sections = Marshal.AllocHGlobal(sizeof(WdbSection) * sectionList.Count);
                var sections = (WdbSection*)wdbFile->Sections;

                for (int i = 0; i < sectionList.Count; i++)
                {
                    sections[i] = new WdbSection();
                    NativeMemoryManager.CopyStringToFixedBuffer(sections[i].Name, 32, sectionList[i].name);

                    if (sectionList[i].data != null)
                    {
                        sections[i].DataSize = (uint)sectionList[i].data!.Length;
                        sections[i].Data = Marshal.AllocHGlobal(sectionList[i].data.Length);
                        Marshal.Copy(sectionList[i].data, 0, sections[i].Data, sectionList[i].data.Length);
                    }
                }
            }
        }

        private static void ParseRecordsXIII(BinaryReader wdbReader, XIII.WDBVariablesXIII wdbVars, WdbFile* wdbFile)
        {
            wdbFile->Records = Marshal.AllocHGlobal(sizeof(WdbRecord) * (int)wdbVars.RecordCount);
            var records = (WdbRecord*)wdbFile->Records;

            var sectionPos = wdbReader.BaseStream.Position;

            for (int r = 0; r < wdbVars.RecordCount; r++)
            {
                records[r] = new WdbRecord();

                wdbReader.BaseStream.Position = sectionPos;
                var recordName = wdbReader.ReadBytesString(16, false);
                NativeMemoryManager.CopyStringToFixedBuffer(records[r].Name, 16, recordName);

                var recordData = SharedMethods.SaveSectionData(wdbReader, false);

                // Count fields for this record
                var fieldCount = wdbVars.FieldCount;
                records[r].FieldCount = fieldCount;
                records[r].Fields = Marshal.AllocHGlobal(sizeof(WdbField) * (int)fieldCount);
                var fields = (WdbField*)records[r].Fields;

                var strtypelistIndex = 0;
                var recordDataIndex = 0;
                var fieldIndex = 0;

                for (int f = 0; f < wdbVars.FieldCount; f++)
                {
                    fields[fieldIndex] = new WdbField();

                    var fieldName = wdbVars.IsKnown && wdbVars.Fields != null
                        ? wdbVars.Fields[f]
                        : GetDefaultFieldName(wdbVars.StrtypelistValues[strtypelistIndex], f);

                    NativeMemoryManager.CopyStringToFixedBuffer(fields[fieldIndex].Name, 64, fieldName);

                    switch (wdbVars.StrtypelistValues[strtypelistIndex])
                    {
                        case 0: // bitpacked
                            var bitpackedVal = SharedMethods.DeriveUIntFromSectionData(recordData, recordDataIndex, true);
                            fields[fieldIndex].Value.Type = WdbFieldType.Bitpacked;
                            fields[fieldIndex].Value.UIntValue = bitpackedVal;
                            strtypelistIndex++;
                            recordDataIndex += 4;
                            break;

                        case 1: // float
                            var floatVal = SharedMethods.DeriveFloatFromSectionData(recordData, recordDataIndex, true);
                            fields[fieldIndex].Value.Type = WdbFieldType.Float;
                            fields[fieldIndex].Value.FloatValue = floatVal;
                            strtypelistIndex++;
                            recordDataIndex += 4;
                            break;

                        case 2: // string offset
                            var stringOffset = SharedMethods.DeriveUIntFromSectionData(recordData, recordDataIndex, true);
                            var str = SharedMethods.DeriveStringFromArray(wdbVars.StringsData, (int)stringOffset);
                            fields[fieldIndex].Value.Type = WdbFieldType.String;
                            fields[fieldIndex].Value.StringPtr = NativeMemoryManager.AllocateString(str);
                            strtypelistIndex++;
                            recordDataIndex += 4;
                            break;

                        case 3: // uint
                            // Check for uint64
                            if (wdbVars.IsKnown && wdbVars.Fields != null && wdbVars.Fields[f].StartsWith("u64"))
                            {
                                var processArray = new byte[8];
                                Array.ConstrainedCopy(recordData, recordDataIndex, processArray, 0, 8);
                                Array.Reverse(processArray);
                                var uint64Val = BitConverter.ToUInt64(processArray, 0);
                                fields[fieldIndex].Value.Type = WdbFieldType.UInt;
                                fields[fieldIndex].Value.UInt64Value = uint64Val;
                                strtypelistIndex++;
                                recordDataIndex += 8;
                            }
                            else
                            {
                                var uintVal = SharedMethods.DeriveUIntFromSectionData(recordData, recordDataIndex, true);
                                fields[fieldIndex].Value.Type = WdbFieldType.UInt;
                                fields[fieldIndex].Value.UIntValue = uintVal;
                                strtypelistIndex++;
                                recordDataIndex += 4;
                            }
                            break;
                    }

                    fieldIndex++;
                }

                sectionPos += 32;
            }
        }

        private static string GetDefaultFieldName(uint fieldType, int index)
        {
            return fieldType switch
            {
                0 => $"bitpacked-field_{index}",
                1 => $"float-field_{index}",
                2 => $"string-field_{index}",
                3 => $"uint-field_{index}",
                _ => $"unknown-field_{index}"
            };
        }

        #endregion

        #region XIII-2/LR Parser

        public static WdbFileXIII2LR* ParseXIII2LR(string wdbFilePath)
        {
            var wdbVars = new XIII2LR.WDBVariablesXIII2LR();

            using var wdbReader = new BinaryReader(File.Open(wdbFilePath, FileMode.Open, FileAccess.Read, FileShare.Read));

            wdbReader.BaseStream.Position = 0;
            if (wdbReader.ReadBytesString(3, false) != "WPD")
            {
                throw new InvalidDataException("Not a valid WPD file");
            }

            wdbReader.BaseStream.Position += 1;
            wdbVars.RecordCount = wdbReader.ReadBytesUInt32(true);

            if (wdbVars.RecordCount == 0)
            {
                throw new InvalidDataException("No records/sections are present in this file");
            }

            // Parse sections
            XIII2LR.Extraction.SectionsParser.MainSections(wdbReader, wdbVars);

            // Allocate the WdbFileXIII2LR structure
            var wdbFile = (WdbFileXIII2LR*)Marshal.AllocHGlobal(sizeof(WdbFileXIII2LR));
            *wdbFile = new WdbFileXIII2LR();

            // Set sheet name
            NativeMemoryManager.CopyStringToFixedBuffer(wdbFile->SheetName, 64, wdbVars.SheetName);

            wdbFile->RecordCount = wdbVars.RecordCount;
            wdbFile->FieldDefinitionCount = wdbVars.FieldCount;
            wdbFile->HasStrArraySection = (byte)(wdbVars.HasStrArraySection ? 1 : 0);

            // Allocate and copy strtypelist values
            wdbFile->StrtypelistValues = NativeMemoryManager.AllocateIntArray(wdbVars.StrtypelistValues);

            // Allocate field names
            if (wdbVars.Fields != null)
            {
                wdbFile->FieldNames = NativeMemoryManager.AllocateStringArray(wdbVars.Fields, out _);
            }

            // Build sections array
            BuildSectionsXIII2LR(wdbFile, wdbVars);

            // Build strArray entries
            BuildStrArrayEntries(wdbFile, wdbVars);

            // Parse records
            ParseRecordsXIII2LR(wdbReader, wdbVars, wdbFile);

            return wdbFile;
        }

        private static void BuildSectionsXIII2LR(WdbFileXIII2LR* wdbFile, XIII2LR.WDBVariablesXIII2LR wdbVars)
        {
            var sectionList = new List<(string name, byte[]? data)>();

            if (wdbVars.SheetNameData != null)
                sectionList.Add((wdbVars.SheetNameSectionName, wdbVars.SheetNameData));
            if (wdbVars.StringsData != null)
                sectionList.Add((wdbVars.StringSectionName, wdbVars.StringsData));
            if (wdbVars.StrtypelistData != null)
                sectionList.Add((wdbVars.StrtypelistSectionName, wdbVars.StrtypelistData));
            if (wdbVars.TypelistData != null)
                sectionList.Add((wdbVars.TypelistSectionName, wdbVars.TypelistData));
            if (wdbVars.VersionData != null)
                sectionList.Add((wdbVars.VersionSectionName, wdbVars.VersionData));
            if (wdbVars.StrArrayData != null)
                sectionList.Add((wdbVars.StrArraySectionName, wdbVars.StrArrayData));
            if (wdbVars.StrArrayInfoData != null)
                sectionList.Add((wdbVars.StrArrayInfoSectionName, wdbVars.StrArrayInfoData));
            if (wdbVars.StrArrayListData != null)
                sectionList.Add((wdbVars.StrArrayListSectionName, wdbVars.StrArrayListData));
            if (wdbVars.StructItemData != null)
                sectionList.Add((wdbVars.StructItemSectionName, wdbVars.StructItemData));
            if (wdbVars.StructItemNumData != null)
                sectionList.Add((wdbVars.StructItemNumSectionName, wdbVars.StructItemNumData));

            wdbFile->SectionCount = (uint)sectionList.Count;

            if (sectionList.Count > 0)
            {
                wdbFile->Sections = Marshal.AllocHGlobal(sizeof(WdbSection) * sectionList.Count);
                var sections = (WdbSection*)wdbFile->Sections;

                for (int i = 0; i < sectionList.Count; i++)
                {
                    sections[i] = new WdbSection();
                    NativeMemoryManager.CopyStringToFixedBuffer(sections[i].Name, 32, sectionList[i].name);

                    if (sectionList[i].data != null)
                    {
                        sections[i].DataSize = (uint)sectionList[i].data!.Length;
                        sections[i].Data = Marshal.AllocHGlobal(sectionList[i].data.Length);
                        Marshal.Copy(sectionList[i].data, 0, sections[i].Data, sectionList[i].data.Length);
                    }
                }
            }
        }

        private static void BuildStrArrayEntries(WdbFileXIII2LR* wdbFile, XIII2LR.WDBVariablesXIII2LR wdbVars)
        {
            if (wdbVars.StrArrayDict == null || wdbVars.StrArrayDict.Count == 0)
            {
                wdbFile->StrArrayEntryCount = 0;
                wdbFile->StrArrayEntries = IntPtr.Zero;
                return;
            }

            wdbFile->StrArrayEntryCount = (uint)wdbVars.StrArrayDict.Count;
            wdbFile->StrArrayEntries = Marshal.AllocHGlobal(sizeof(WdbStrArrayEntry) * wdbVars.StrArrayDict.Count);
            var entries = (WdbStrArrayEntry*)wdbFile->StrArrayEntries;

            int i = 0;
            foreach (var kvp in wdbVars.StrArrayDict)
            {
                entries[i] = new WdbStrArrayEntry();
                NativeMemoryManager.CopyStringToFixedBuffer(entries[i].Key, 64, kvp.Key);
                entries[i].StringCount = (uint)kvp.Value.Count;

                if (kvp.Value.Count > 0)
                {
                    entries[i].Strings = Marshal.AllocHGlobal(IntPtr.Size * kvp.Value.Count);
                    var strings = (IntPtr*)entries[i].Strings;
                    for (int j = 0; j < kvp.Value.Count; j++)
                    {
                        strings[j] = NativeMemoryManager.AllocateString(kvp.Value[j]);
                    }
                }
                i++;
            }
        }

        private static void ParseRecordsXIII2LR(BinaryReader wdbReader, XIII2LR.WDBVariablesXIII2LR wdbVars, WdbFileXIII2LR* wdbFile)
        {
            wdbFile->Records = Marshal.AllocHGlobal(sizeof(WdbRecord) * (int)wdbVars.RecordCount);
            var records = (WdbRecord*)wdbFile->Records;

            var sectionPos = wdbReader.BaseStream.Position;

            for (int r = 0; r < wdbVars.RecordCount; r++)
            {
                records[r] = new WdbRecord();

                wdbReader.BaseStream.Position = sectionPos;
                var recordName = wdbReader.ReadBytesString(16, false);
                NativeMemoryManager.CopyStringToFixedBuffer(records[r].Name, 16, recordName);

                var recordData = SharedMethods.SaveSectionData(wdbReader, false);

                var fieldCount = wdbVars.FieldCount;
                records[r].FieldCount = fieldCount;
                records[r].Fields = Marshal.AllocHGlobal(sizeof(WdbField) * (int)fieldCount);
                var fields = (WdbField*)records[r].Fields;

                var strtypelistIndex = 0;
                var recordDataIndex = 0;
                var fieldIndex = 0;

                for (int f = 0; f < wdbVars.FieldCount; f++)
                {
                    fields[fieldIndex] = new WdbField();

                    var fieldName = wdbVars.Fields != null ? wdbVars.Fields[f] : $"field_{f}";
                    NativeMemoryManager.CopyStringToFixedBuffer(fields[fieldIndex].Name, 64, fieldName);

                    switch (wdbVars.StrtypelistValues[strtypelistIndex])
                    {
                        case 0: // bitpacked
                            var bitpackedVal = SharedMethods.DeriveUIntFromSectionData(recordData, recordDataIndex, true);
                            fields[fieldIndex].Value.Type = WdbFieldType.Bitpacked;
                            fields[fieldIndex].Value.UIntValue = bitpackedVal;
                            strtypelistIndex++;
                            recordDataIndex += 4;
                            break;

                        case 1: // float
                            var floatVal = SharedMethods.DeriveFloatFromSectionData(recordData, recordDataIndex, true);
                            fields[fieldIndex].Value.Type = WdbFieldType.Float;
                            fields[fieldIndex].Value.FloatValue = floatVal;
                            strtypelistIndex++;
                            recordDataIndex += 4;
                            break;

                        case 2: // string offset
                            var stringOffset = SharedMethods.DeriveUIntFromSectionData(recordData, recordDataIndex, true);
                            var str = SharedMethods.DeriveStringFromArray(wdbVars.StringsData, (int)stringOffset);
                            fields[fieldIndex].Value.Type = WdbFieldType.String;
                            fields[fieldIndex].Value.StringPtr = NativeMemoryManager.AllocateString(str);
                            strtypelistIndex++;
                            recordDataIndex += 4;
                            break;

                        case 3: // uint
                            var uintVal = SharedMethods.DeriveUIntFromSectionData(recordData, recordDataIndex, true);
                            fields[fieldIndex].Value.Type = WdbFieldType.UInt;
                            fields[fieldIndex].Value.UIntValue = uintVal;
                            strtypelistIndex++;
                            recordDataIndex += 4;
                            break;
                    }

                    fieldIndex++;
                }

                sectionPos += 32;
            }
        }

        #endregion
    }
}
