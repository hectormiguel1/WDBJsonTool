using System.Text;
using WDBJsonTool.Support;

namespace WDBJsonTool.XIII.Conversion
{
    internal abstract class WDBbuilder
    {
        public static void BuildWDB(DataStructures.WDBFile wdbFile, string filePath)
        {

            Log.Fine($"Building wdb file from WDBFile object to {filePath}....");

            var wdbVars = new WDBVariablesXIII();
            wdbVars.WDBName = wdbFile.WDBName;
            wdbVars.WDBFilePath = filePath;
            
            // Access header section
            if (!wdbFile.Sections.TryGetValue(JsonVariables.HeaderSectionToken, out var headerSection))
            {
                Log.Fatal("Header section not found in WDBFile.");
                return;
            }
            
            Log.Finest($"Loaded header sections: {headerSection}");
            // Populate RecordCount
            if (headerSection.TryGetValue(JsonVariables.RecordCountToken, out var recordCountObj))
            {
                wdbVars.RecordCount = Convert.ToUInt32(recordCountObj);
            }
            else
            {
                wdbVars.RecordCount = (uint)wdbFile.Records.Count;
            }
            Log.Finest($"Loaded record count: {wdbVars.RecordCount}");
            wdbVars.RecordCountWithSections = wdbVars.RecordCount + 4;

            // Populate !!strtypelist
            if (headerSection.TryGetValue(WDBVariablesXIII.StrtypelistSectionName, out var strTypelistObj))
            {
                wdbVars.StrtypelistValues = strTypelistObj switch
                {
                    List<uint> strTypelist => strTypelist,
                    List<int> strTypelistInt => strTypelistInt.Select(i => (uint)i).ToList(),
                    _ => wdbVars.StrtypelistValues
                };
                wdbVars.StrtypelistData = ConvertUIntListToByteArray(wdbVars.StrtypelistValues);
            }
            else
            {
                wdbVars.StrtypelistData = [];
            }
            Log.Finest($"Loaded strTypeList: {strTypelistObj}");
            // Populate !!typelist
            if (headerSection.TryGetValue(WDBVariablesXIII.TypelistSectionName, out var typelistObj))
            {
                wdbVars.TypelistValues = typelistObj switch
                {
                    List<uint> typelist => typelist,
                    List<int> typelistInt => typelistInt.Select(i => (uint)i).ToList(),
                    _ => wdbVars.TypelistValues
                };
                wdbVars.TypelistData = ConvertUIntListToByteArray(wdbVars.TypelistValues);
            }
            else
            {
                wdbVars.TypelistData = [];
            }
            Log.Finest($"Loaded typelists: {typelistObj}");

            // Populate !!version
            if (headerSection.TryGetValue(WDBVariablesXIII.VersionSectionName, out var versionObj))
            {
                var version = Convert.ToUInt32(versionObj);
                wdbVars.VersionData = BitConverter.GetBytes(version);
                if (BitConverter.IsLittleEndian) Array.Reverse(wdbVars.VersionData);
            }
            else
            {
                wdbVars.VersionData = new byte[4];
            }

            // Populate !structitem (Fields)
            if (headerSection.TryGetValue(WDBVariablesXIII.StructItemSectionName, out var structItemObj))
            {
                if (structItemObj is List<string> fieldsList)
                {
                    wdbVars.Fields = fieldsList.ToArray();
                    wdbVars.FieldCount = (uint)wdbVars.Fields.Length;
                }
            }
            
            // Populate RecordsDataDict
            wdbVars.RecordsDataDict = new Dictionary<string, List<object>>();
            foreach (var record in wdbFile.Records)
            {
                var recordName = record.ContainsKey(JsonVariables.RecordToken) ? record[JsonVariables.RecordToken].ToString() : "";
                
                if (string.IsNullOrEmpty(recordName)) continue;

                var valuesList = new List<object>();
                if (wdbVars.Fields != null)
                {
                    foreach (var field in wdbVars.Fields)
                    {
                        if (record.TryGetValue(field, out var val))
                        {
                            valuesList.Add(val);
                        }
                        else
                        {
                            // Handle missing field? Default to 0/null?
                            Log.Warning($"Field {field} missing in record {recordName}. Defaulting to 0.");
                            valuesList.Add(0); 
                        }
                    }
                }
                wdbVars.RecordsDataDict.Add(recordName, valuesList);
            }

            // Convert Records to Bytes
            if (wdbVars.Fields is { Length: > 0 })
            {
                RecordsConversion.ConvertRecordsWithFields(wdbVars);
            }
            else
            {
                RecordsConversion.ConvertRecordsNoFields(wdbVars);
            }

            // Build Strings Data from ProcessedStringsDict (populated by RecordsConversion)
            BuildStringsSection(wdbVars);

            // Call the original BuildWDB method
            BuildWDB(wdbVars);
        }

        private static void BuildStringsSection(WDBVariablesXIII wdbVars)
        {
            using (var ms = new MemoryStream())
            {
                // Write the empty string (offset 0)
                ms.WriteByte(0);

                // Write all other strings sorted by their assigned offset
                foreach (var entry in wdbVars.ProcessedStringsDict.OrderBy(x => x.Value))
                {
                    if (string.IsNullOrEmpty(entry.Key)) continue; // Already handled empty string

                    var stringBytes = Encoding.UTF8.GetBytes(entry.Key + "\0");
                    ms.Write(stringBytes, 0, stringBytes.Length);
                }
                wdbVars.StringsData = ms.ToArray();
            }
            wdbVars.HasStringSection = wdbVars.StringsData.Length > 1;
        }

        private static byte[] ConvertUIntListToByteArray(List<uint> list)
        {
            if (list.Count == 0) return [];

            var byteArray = new byte[list.Count * 4];
            for (var i = 0; i < list.Count; i++)
            {
                var valBytes = BitConverter.GetBytes(list[i]);
                if (BitConverter.IsLittleEndian) Array.Reverse(valBytes); // Ensure big-endian as per WDB format
                Buffer.BlockCopy(valBytes, 0, byteArray, i * 4, 4);
            }
            return byteArray;
        }

        public static void BuildWDB(WDBVariablesXIII wdbVars)
        {
           Log.Fine("Building wdb file....");

            if (File.Exists(wdbVars.WDBFilePath))
            {
                File.Delete(wdbVars.WDBFilePath);
            }

            using (var outWDBwriter = new BinaryWriter(File.Open(wdbVars.WDBFilePath, FileMode.Append, FileAccess.Write)))
            {
                outWDBwriter.Write(Encoding.UTF8.GetBytes("WPD\0"));
                outWDBwriter.WriteBytesUInt32(wdbVars.RecordCountWithSections, true);
                outWDBwriter.BaseStream.PadNull(8);

                // string
                WriteSectionName(outWDBwriter, WDBVariablesXIII.StringSectionName, WDBVariablesXIII.StringSectionNameLength);

                // strtypelist
                WriteSectionName(outWDBwriter, WDBVariablesXIII.StrtypelistSectionName, WDBVariablesXIII.StrtypelistSectionNameLength);

                // typelist
                WriteSectionName(outWDBwriter, WDBVariablesXIII.TypelistSectionName, WDBVariablesXIII.TypelistSectionNameLength);

                // version
                WriteSectionName(outWDBwriter, WDBVariablesXIII.VersionSectionName, WDBVariablesXIII.VersionSectionNameLength);

                // record names
                foreach (var recordNameBytes in wdbVars.RecordsDataDict.Keys.Select(recordName => Encoding.UTF8.GetBytes(recordName)))
                {
                    outWDBwriter.Write(recordNameBytes);

                    outWDBwriter.BaseStream.PadNull(16 - recordNameBytes.Length);
                    outWDBwriter.BaseStream.PadNull(16);
                }
            }


            // Start writing the data and update offsets
            using (var outWDBdataWriter = new BinaryWriter(File.Open(wdbVars.WDBFilePath, FileMode.Open, FileAccess.Write)))
            {
                uint secPos = 0;
                long offsetUpdatePos = 32;

                // string 
                outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                secPos = (uint)outWDBdataWriter.BaseStream.Position;

                if (wdbVars.HasStringSection)
                {
                    uint stringSectionSize = 0;

                    foreach (var stringKey in wdbVars.ProcessedStringsDict.Keys)
                    {
                        if (stringKey == "")
                        {
                            outWDBdataWriter.Write((byte)0);
                            stringSectionSize++;
                        }
                        else
                        {
                            var stringKeyBytes = Encoding.UTF8.GetBytes(stringKey + "\0");
                            outWDBdataWriter.Write(stringKeyBytes);
                            stringSectionSize += (uint)stringKeyBytes.Length;
                        }
                    }

                    PadBytesAfterSection(outWDBdataWriter);
                    UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, stringSectionSize);
                }
                else
                {
                    outWDBdataWriter.Write((byte)0);
                    PadBytesAfterSection(outWDBdataWriter);
                    UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, 1);
                }

                offsetUpdatePos += 32;


                // strtypelist
                outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                secPos = (uint)outWDBdataWriter.BaseStream.Position;
                outWDBdataWriter.Write(wdbVars.StrtypelistData);

                PadBytesAfterSection(outWDBdataWriter);

                UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, (uint)wdbVars.StrtypelistData.Length);
                offsetUpdatePos += 32;


                // typelist
                outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                secPos = (uint)outWDBdataWriter.BaseStream.Position;
                outWDBdataWriter.Write(wdbVars.TypelistData);

                PadBytesAfterSection(outWDBdataWriter);

                UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, (uint)wdbVars.TypelistData.Length);
                offsetUpdatePos += 32;


                // version
                outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                secPos = (uint)outWDBdataWriter.BaseStream.Position;
                outWDBdataWriter.Write(wdbVars.VersionData);

                PadBytesAfterSection(outWDBdataWriter);

                UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, (uint)wdbVars.VersionData.Length);
                offsetUpdatePos += 32;


                // records
                foreach (var currentRecordData in wdbVars.OutPerRecordData.Keys.Select(recordkey => wdbVars.OutPerRecordData[recordkey]))
                {
                    outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                    secPos = (uint)outWDBdataWriter.BaseStream.Position;
                    outWDBdataWriter.Write(currentRecordData);

                    PadBytesAfterSection(outWDBdataWriter);

                    UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, (uint)currentRecordData.Length);
                    offsetUpdatePos += 32;
                }
            }
        }


        private static void WriteSectionName(BinaryWriter outWDBwriter, string nameString, int nameLength)
        {
            outWDBwriter.Write(Encoding.UTF8.GetBytes(nameString));
            outWDBwriter.BaseStream.PadNull(16 - nameLength);
            outWDBwriter.BaseStream.PadNull(16);
        }


        private static void PadBytesAfterSection(BinaryWriter outWDBdataWriter)
        {
            var currentPos = outWDBdataWriter.BaseStream.Length;
            var padValue = 4;

            if (currentPos % padValue != 0)
            {
                var remainder = currentPos % padValue;
                var increaseBytes = padValue - remainder;
                var newPos = currentPos + increaseBytes;
                var nullBytesAmount = newPos - currentPos;

                outWDBdataWriter.BaseStream.PadNull((int)nullBytesAmount);
            }
        }


        private static void UpdateOffsets(BinaryWriter outWDBdataWriter, long pos, uint secPos, uint size)
        {
            outWDBdataWriter.BaseStream.Position = pos;
            outWDBdataWriter.WriteBytesUInt32(secPos, true);
            outWDBdataWriter.WriteBytesUInt32(size, true);
        }
    }
}