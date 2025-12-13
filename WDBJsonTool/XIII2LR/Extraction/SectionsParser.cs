using System.Text;
using System.Text.Json;
using WDBJsonTool.Support;
using WDBJsonTool.DataStructures; // Added
using WDBJsonTool; // Added

namespace WDBJsonTool.XIII2LR.Extraction
{
    internal abstract class SectionsParser
    {
        public static void MainSections(BinaryReader wdbReader, WDBVariablesXIII2LR wdbVars)
        {
            // Parse main sections
            long currentSectionNamePos = 16;
            string sectioNameRead;

            wdbVars.StrtypelistData = [];
            wdbVars.StructItemData = [];
            wdbVars.FieldCount = 0;


            while (true)
            {
                wdbReader.BaseStream.Position = currentSectionNamePos;
                sectioNameRead = wdbReader.ReadBytesString(16, false);

                // Break the loop if its
                // not a valid "!" section
                if (!sectioNameRead.StartsWith("!"))
                {
                    _ = wdbReader.BaseStream.Position = currentSectionNamePos;
                    break;
                }

                // !!sheetname
                if (sectioNameRead == wdbVars.SheetNameSectionName)
                {
                    _ = wdbReader.BaseStream.Position = wdbReader.ReadBytesUInt32(true);
                    wdbVars.SheetName = wdbReader.ReadStringTillNull();
                    wdbVars.RecordCount--;
                }

                // !!strArray
                if (sectioNameRead == wdbVars.StrArraySectionName)
                {
                    wdbVars.HasStrArraySection = true;

                    _ = wdbReader.BaseStream.Position = currentSectionNamePos;
                    StrArrayParser.SubSections(wdbReader, wdbVars);
                }

                // !!string
                if (sectioNameRead == wdbVars.StringSectionName)
                {
                    wdbVars.HasStringSection = true;

                    wdbVars.StringsData = SharedMethods.SaveSectionData(wdbReader, false);
                    wdbVars.RecordCount--;
                }

                // !!strtypelist
                if (sectioNameRead == wdbVars.StrtypelistSectionName)
                {
                    wdbVars.ParseStrtypelistAsV1 = true;
                    wdbVars.StrtypelistData = SharedMethods.SaveSectionData(wdbReader, false);
                    wdbVars.RecordCount--;
                }

                // !!strtypelistb
                if (sectioNameRead == wdbVars.StrtypelistbSectionName)
                {
                    wdbVars.ParseStrtypelistAsV1 = false;
                    wdbVars.StrtypelistData = SharedMethods.SaveSectionData(wdbReader, false);
                    wdbVars.RecordCount--;
                }

                // !!typelist
                if (sectioNameRead == wdbVars.TypelistSectionName)
                {
                    wdbVars.HasTypelistSection = true;
                    wdbVars.TypelistData = SharedMethods.SaveSectionData(wdbReader, false);
                    wdbVars.RecordCount--;
                }

                // !!version
                if (sectioNameRead == wdbVars.VersionSectionName)
                {
                    wdbVars.VersionData = SharedMethods.SaveSectionData(wdbReader, false);
                    wdbVars.RecordCount--;
                }

                // !structitem
                if (sectioNameRead == wdbVars.StructItemSectionName)
                {
                    wdbVars.StructItemData = SharedMethods.SaveSectionData(wdbReader, false);
                    wdbVars.RecordCount--;
                }

                // !structitemnum
                if (sectioNameRead == wdbVars.StructItemNumSectionName)
                {
                    wdbVars.FieldCount = BitConverter.ToUInt32(SharedMethods.SaveSectionData(wdbReader, true), 0);
                    wdbVars.RecordCount--;
                }

                currentSectionNamePos += 32;
            }


            // Check if the important 
            // sections are all parsed
            var imptSectionsParsed = wdbVars.StrtypelistData.Length != 0 && wdbVars.StructItemData.Length != 0 && wdbVars.FieldCount != 0;

            if (!imptSectionsParsed)
            {
                SharedMethods.ErrorExit("Necessary sections were unable to be processed correctly.");
            }

            if (wdbVars.SheetName == "" || wdbVars.SheetName == null)
            {
                wdbVars.SheetName = "Not Specified";
            }



            Log.Info($"{wdbVars.SheetNameSectionName}: {wdbVars.SheetName}"); // Replaced Console.WriteLine




            // Process !structitem data
            wdbVars.Fields = new string[wdbVars.FieldCount];
            var stringStartPos = 0;

            for (var sf = 0; sf < wdbVars.FieldCount; sf++)
            {
                var derivedString = SharedMethods.DeriveStringFromArray(wdbVars.StructItemData, stringStartPos);

                if (derivedString == "")
                {
                    SharedMethods.ErrorExit("Detected a null string structitem.");
                }

                wdbVars.Fields[sf] = derivedString;

                stringStartPos += Encoding.UTF8.GetByteCount(derivedString) + 1;
            }


            // Process strArray sections
            // data
            if (wdbVars.HasStrArraySection)
            {
                Log.Info($"Organizing {wdbVars.StrArraySectionName} data...."); // Replaced Console.WriteLine

                StrArrayParser.ArrangeArrayData(wdbVars);

    
    
            }
        }


        public static WDBSection ParseSectionsToWDBSection(WDBVariablesXIII2LR wdbVars)
        {
            var sectionData = new WDBSection();

            sectionData[JsonVariables.RecordCountToken] = wdbVars.RecordCount;
            sectionData[wdbVars.SheetNameSectionName] = wdbVars.SheetName;
            sectionData[JsonVariables.HasStrArrayToken] = wdbVars.HasStrArraySection;


            // Write array info values
            // if strArray section is
            // present
            if (wdbVars.HasStrArraySection)
            {
                sectionData[JsonVariables.BitsPerOffsetToken] = wdbVars.BitsPerOffset;
                sectionData[JsonVariables.OffsetsPerValueToken] = wdbVars.OffsetsPerValue;
            }


            // Parse and write the strtypelistData
            sectionData[JsonVariables.IsStrTypelistV1Token] = wdbVars.ParseStrtypelistAsV1;

            List<int> strtypelistValues = []; // Using List<int> to store values

            var strtypelistbIndex = 0;
            var currentStrtypelistData = new byte[4];
            var strtypelistIndexAdjust = wdbVars.ParseStrtypelistAsV1 ? 4 : 1;
            var strTypelistValueCount = wdbVars.ParseStrtypelistAsV1 ? wdbVars.StrtypelistData.Length / 4 : wdbVars.StrtypelistData.Length;
            int strtypelistValue;

            for (var s = 0; s < strTypelistValueCount; s++)
            {
                if (wdbVars.ParseStrtypelistAsV1)
                {
                    Array.ConstrainedCopy(wdbVars.StrtypelistData, strtypelistbIndex, currentStrtypelistData, 0, 4);
                    Array.Reverse(currentStrtypelistData);
                    strtypelistValue = (int)BitConverter.ToUInt32(currentStrtypelistData, 0);
                }
                else
                {
                    strtypelistValue = wdbVars.StrtypelistData[strtypelistbIndex];
                }

                wdbVars.StrtypelistValues.Add(strtypelistValue); // Keep this to populate wdbVars
                strtypelistValues.Add(strtypelistValue); // Add to the new list
                strtypelistbIndex += strtypelistIndexAdjust;
            }

            if (wdbVars.ParseStrtypelistAsV1)
            {
                sectionData[wdbVars.StrtypelistSectionName] = strtypelistValues;
            }
            else
            {
                sectionData[wdbVars.StrtypelistbSectionName] = strtypelistValues;
            }


            // Write all the typelist data
            sectionData[JsonVariables.HasTypelistToken] = wdbVars.HasTypelistSection;
            if (wdbVars.HasTypelistSection)
            {
                List<int> typelistValues = []; // Using List<int> to store values

                var typelistbIndex = 0;
                var currentTypelistData = new byte[4];
                int typelistValue;

                for (var t = 0; t < wdbVars.TypelistData.Length / 4; t++)
                {
                    Array.ConstrainedCopy(wdbVars.TypelistData, typelistbIndex, currentTypelistData, 0, 4);
                    Array.Reverse(currentTypelistData);
                    typelistValue = (int)BitConverter.ToUInt32(currentTypelistData, 0);

                    typelistValues.Add(typelistValue); // Add to the new list
                    typelistbIndex += 4;
                }

                sectionData[wdbVars.TypelistSectionName] = typelistValues;
            }


            // Write version data
            sectionData[wdbVars.VersionSectionName] = SharedMethods.DeriveUIntFromSectionData(wdbVars.VersionData, 0, true);


            // Write structitem data
            List<string> fields = []; // Using List<string> to store values

            for (var i = 0; i < wdbVars.FieldCount; i++)
            {
                fields.Add(wdbVars.Fields[i]); // Add to the new list
            }

            sectionData[wdbVars.StructItemSectionName] = fields;

            return sectionData;
        }
    }
}