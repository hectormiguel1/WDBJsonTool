using System.Text;

using WDBJsonTool.Extensions;
namespace WDBJsonTool.XIII2LR.Conversion;
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
                WriteSectionName(outWDBwriter, WDBVariablesXIII2LR.SheetNameSectionName, WDBVariablesXIII2LR.SheetNameSectionNameLength);
            }

            // strarray
            if (wdbVars.HasStrArraySection)
            {
                WriteSectionName(outWDBwriter, WDBVariablesXIII2LR.StrArraySectionName, WDBVariablesXIII2LR.StrArraySectionNameLength);
                WriteSectionName(outWDBwriter, WDBVariablesXIII2LR.StrArrayInfoSectionName, WDBVariablesXIII2LR.StrArrayInfoSectionNameLength);
                WriteSectionName(outWDBwriter, WDBVariablesXIII2LR.StrArrayListSectionName, WDBVariablesXIII2LR.StrArrayListSectionNameLength);
            }

            // string
            if (wdbVars.HasStringSection)
            {
                WriteSectionName(outWDBwriter, WDBVariablesXIII2LR.StringSectionName, WDBVariablesXIII2LR.StringSectionNameLength);
            }

            // strtypelist
            var strtypelistSectionName = wdbVars.ParseStrtypelistAsV1 ? WDBVariablesXIII2LR.StrtypelistSectionName : WDBVariablesXIII2LR.StrtypelistbSectionName;
            var strtypelistSectionNameLength = wdbVars.ParseStrtypelistAsV1 ? WDBVariablesXIII2LR.StrtypelistSectionNameLength : WDBVariablesXIII2LR.StrtypelistbSectionNameLength;
            WriteSectionName(outWDBwriter, strtypelistSectionName, strtypelistSectionNameLength);

            // typelist
            if (wdbVars.HasTypelistSection)
            {
                WriteSectionName(outWDBwriter, WDBVariablesXIII2LR.TypelistSectionName, WDBVariablesXIII2LR.TypelistSectionNameLength);
            }

            // version
            WriteSectionName(outWDBwriter, WDBVariablesXIII2LR.VersionSectionName, WDBVariablesXIII2LR.VersionSectionNameLength);

            // structitem
            WriteSectionName(outWDBwriter, WDBVariablesXIII2LR.StructItemSectionName, WDBVariablesXIII2LR.StructItemSectionNameLength);

            // structitemnum
            WriteSectionName(outWDBwriter, WDBVariablesXIII2LR.StructItemNumSectionName, WDBVariablesXIII2LR.StructItemNumSectionNameLength);

            // record names
            foreach (var recordNameBytes in wdbVars.RecordsDataDict.Keys.Select(recordName => Encoding.UTF8.GetBytes(recordName)))
            {
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
        const int padValue = 4;

        if (currentPos % padValue == 0) return;
        var remainder = currentPos % padValue;
        var increaseBytes = padValue - remainder;
        var newPos = currentPos + increaseBytes;
        var nullBytesAmount = newPos - currentPos;

        outWDBdataWriter.BaseStream.PadNull((int)nullBytesAmount);
    }


    private static void UpdateOffsets(BinaryWriter outWDBdataWriter, long pos, uint secPos, uint size)
    {
        outWDBdataWriter.BaseStream.Position = pos;
        outWDBdataWriter.WriteBytesUInt32(secPos, true);
        outWDBdataWriter.WriteBytesUInt32(size, true);
    }
}
