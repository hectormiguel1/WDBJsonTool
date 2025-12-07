using System.Text;

namespace WDBJsonTool.XIII2LR.Conversion
{
    internal class WDBbuilder
    {
        public static void BuildWDB(WDBVariablesXIII2LR wdbVars)
        {
            // Build base wdb file
            if (File.Exists(wdbVars.WDBFilePath))
            {
                File.Delete(wdbVars.WDBFilePath);
            }

            using (var outWDBwriter = new BinaryWriter(File.Open(wdbVars.WDBFilePath, FileMode.Append, FileAccess.Write)))
            {
                outWDBwriter.Write(Encoding.UTF8.GetBytes("WPD\0"));
                outWDBwriter.WriteBytesUInt32(wdbVars.RecordCountWithSections, true);
                outWDBwriter.BaseStream.PadNull(8);

                // sheetname
                if (wdbVars.SheetName != "Not Specified")
                {
                    WriteSectionName(outWDBwriter, wdbVars.SheetNameSectionName, wdbVars.SheetNameSectionNameLength);
                }

                // strarray
                if (wdbVars.HasStrArraySection)
                {
                    WriteSectionName(outWDBwriter, wdbVars.StrArraySectionName, wdbVars.StrArraySectionNameLength);
                    WriteSectionName(outWDBwriter, wdbVars.StrArrayInfoSectionName, wdbVars.StrArrayInfoSectionNameLength);
                    WriteSectionName(outWDBwriter, wdbVars.StrArrayListSectionName, wdbVars.StrArrayListSectionNameLength);
                }

                // string
                if (wdbVars.HasStringSection)
                {
                    WriteSectionName(outWDBwriter, wdbVars.StringSectionName, wdbVars.StringSectionNameLength);
                }

                // strtypelist
                var strtypelistSectionName = wdbVars.ParseStrtypelistAsV1 ? wdbVars.StrtypelistSectionName : wdbVars.StrtypelistbSectionName;
                var strtypelistSectionNameLength = wdbVars.ParseStrtypelistAsV1 ? wdbVars.StrtypelistSectionNameLength : wdbVars.StrtypelistbSectionNameLength;
                WriteSectionName(outWDBwriter, strtypelistSectionName, strtypelistSectionNameLength);

                // typelist
                if (wdbVars.HasTypelistSection)
                {
                    WriteSectionName(outWDBwriter, wdbVars.TypelistSectionName, wdbVars.TypelistSectionNameLength);
                }

                // version
                WriteSectionName(outWDBwriter, wdbVars.VersionSectionName, wdbVars.VersionSectionNameLength);

                // structitem
                WriteSectionName(outWDBwriter, wdbVars.StructItemSectionName, wdbVars.StructItemSectionNameLength);

                // structitemnum
                WriteSectionName(outWDBwriter, wdbVars.StructItemNumSectionName, wdbVars.StructItemNumSectionNameLength);

                // record names
                foreach (var recordName in wdbVars.RecordsDataDict.Keys)
                {
                    var recordNameBytes = Encoding.UTF8.GetBytes(recordName);
                    outWDBwriter.Write(recordNameBytes);

                    outWDBwriter.BaseStream.PadNull(16 - recordNameBytes.Length);
                    outWDBwriter.BaseStream.PadNull(16);
                }
            }


            // Start writing the data and
            // update offsets
            using (var outWDBdataWriter = new BinaryWriter(File.Open(wdbVars.WDBFilePath, FileMode.Open, FileAccess.Write)))
            {
                uint secPos = 0;
                long offsetUpdatePos = 32;

                // sheetname
                if (wdbVars.SheetName != "Not Specified")
                {
                    outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                    secPos = (uint)outWDBdataWriter.BaseStream.Position;

                    var sheetNameBytes = Encoding.UTF8.GetBytes(wdbVars.SheetName);

                    outWDBdataWriter.Write(sheetNameBytes);
                    PadBytesAfterSection(outWDBdataWriter);

                    UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, (uint)sheetNameBytes.Length);
                    offsetUpdatePos += 32;
                }


                // strArray sections
                if (wdbVars.HasStrArraySection)
                {
                    // strArray
                    outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                    secPos = (uint)outWDBdataWriter.BaseStream.Position;
                    outWDBdataWriter.Write(wdbVars.StrArrayData);

                    PadBytesAfterSection(outWDBdataWriter);

                    UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, (uint)wdbVars.StrArrayData.Length);
                    offsetUpdatePos += 32;


                    // strArrayInfo
                    outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                    secPos = (uint)outWDBdataWriter.BaseStream.Position;
                    outWDBdataWriter.Write(wdbVars.StrArrayInfoData);

                    PadBytesAfterSection(outWDBdataWriter);

                    UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, (uint)wdbVars.StrArrayInfoData.Length);
                    offsetUpdatePos += 32;


                    // strArrayList
                    outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                    secPos = (uint)outWDBdataWriter.BaseStream.Position;
                    outWDBdataWriter.Write(wdbVars.StrArrayListData);

                    PadBytesAfterSection(outWDBdataWriter);

                    UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, (uint)wdbVars.StrArrayListData.Length);
                    offsetUpdatePos += 32;
                }


                // string 
                if (wdbVars.HasStringSection)
                {
                    outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                    secPos = (uint)outWDBdataWriter.BaseStream.Position;

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
                    offsetUpdatePos += 32;
                }


                // strtypelist
                outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                secPos = (uint)outWDBdataWriter.BaseStream.Position;
                outWDBdataWriter.Write(wdbVars.StrtypelistData);

                PadBytesAfterSection(outWDBdataWriter);

                UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, (uint)wdbVars.StrtypelistData.Length);
                offsetUpdatePos += 32;


                // typelist
                if (wdbVars.HasTypelistSection)
                {
                    outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                    secPos = (uint)outWDBdataWriter.BaseStream.Position;
                    outWDBdataWriter.Write(wdbVars.TypelistData);

                    PadBytesAfterSection(outWDBdataWriter);

                    UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, (uint)wdbVars.TypelistData.Length);
                    offsetUpdatePos += 32;
                }


                // version
                outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                secPos = (uint)outWDBdataWriter.BaseStream.Position;
                outWDBdataWriter.Write(wdbVars.VersionData);

                PadBytesAfterSection(outWDBdataWriter);

                UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, (uint)wdbVars.VersionData.Length);
                offsetUpdatePos += 32;


                // structitem
                outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                secPos = (uint)outWDBdataWriter.BaseStream.Position;
                outWDBdataWriter.Write(wdbVars.StructItemData);

                PadBytesAfterSection(outWDBdataWriter);

                UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, (uint)wdbVars.StructItemData.Length);
                offsetUpdatePos += 32;


                // structitemnum
                outWDBdataWriter.BaseStream.Position = outWDBdataWriter.BaseStream.Length;
                secPos = (uint)outWDBdataWriter.BaseStream.Position;
                outWDBdataWriter.Write(wdbVars.StructItemNumData);

                PadBytesAfterSection(outWDBdataWriter);

                UpdateOffsets(outWDBdataWriter, offsetUpdatePos, secPos, 4);
                offsetUpdatePos += 32;


                // records
                foreach (var recordkey in wdbVars.OutPerRecordData.Keys)
                {
                    var currentRecordData = wdbVars.OutPerRecordData[recordkey];

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