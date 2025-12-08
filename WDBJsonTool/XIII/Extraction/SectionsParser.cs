using System.Text.Json;
using WDBJsonTool.Support;
using WDBJsonTool.DataStructures; // Added
using WDBJsonTool; // Added

namespace WDBJsonTool.XIII.Extraction
{
    internal class SectionsParser
    {
        public static void MainSections(BinaryReader wdbReader, WDBVariablesXIII wdbVars)
        {
            // Parse main sections
            long currentSectionNamePos = 16;
            string sectioNameRead;

            while (true)
            {
                wdbReader.BaseStream.Position = currentSectionNamePos;
                sectioNameRead = wdbReader.ReadBytesString(16, false);

                // Break the loop if its
                // not a valid "!" section
                if (!sectioNameRead.StartsWith("!!"))
                {
                    _ = wdbReader.BaseStream.Position = currentSectionNamePos;
                    break;
                }

                // !!sheetname check
                if (sectioNameRead == "!!sheetname")
                {
                    SharedMethods.ErrorExit("Specified WDB file is from XIII-2 or LR. set the gamecode to -ff132 to extract this file.");
                }

                // !!strArray check
                if (sectioNameRead == "!!strArray")
                {
                    SharedMethods.ErrorExit("Specified WDB file is from XIII-2 or LR. set the gamecode to -ff132 to extract this file.");
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
                    wdbVars.StrtypelistData = SharedMethods.SaveSectionData(wdbReader, false);

                    if (wdbVars.StrtypelistData.Length != 0)
                    {
                        wdbVars.StrtypelistValues = SharedMethods.GetSectionDataValues(wdbVars.StrtypelistData);
                        wdbVars.FieldCount = (uint)wdbVars.StrtypelistValues.Count;
                    }

                    wdbVars.RecordCount--;
                }

                // !!typelist
                if (sectioNameRead == wdbVars.TypelistSectionName)
                {
                    wdbVars.TypelistData = SharedMethods.SaveSectionData(wdbReader, false);

                    if (wdbVars.TypelistData.Length != 0)
                    {
                        wdbVars.TypelistValues = SharedMethods.GetSectionDataValues(wdbVars.TypelistData);
                    }

                    wdbVars.RecordCount--;
                }

                // !!version
                if (sectioNameRead == wdbVars.VersionSectionName)
                {
                    wdbVars.VersionData = SharedMethods.SaveSectionData(wdbReader, false);
                    wdbVars.RecordCount--;
                }

                currentSectionNamePos += 32;
            }

            // Check if the !!strtypelist
            // is parsed 
            if (wdbVars.StrtypelistData.Length == 0)
            {
                SharedMethods.ErrorExit("!!strtypelist section was not present in the file.");
            }
        }


        public static WDBSection ParseSectionsToWDBSection(WDBVariablesXIII wdbVars)
        {
            WDBSection sectionData = new WDBSection();

            sectionData[JsonVariables.RecordCountToken] = wdbVars.RecordCount;

            if (WDBDicts.RecordIDs.ContainsKey(wdbVars.WDBName) && !wdbVars.IgnoreKnown)
            {
                wdbVars.IsKnown = true;
                sectionData[JsonVariables.IsKnownToken] = wdbVars.IsKnown;

                wdbVars.SheetName = WDBDicts.RecordIDs[wdbVars.WDBName];
                sectionData[wdbVars.SheetNameSectionName] = wdbVars.SheetName;

                Log.Info("");
                Log.Info("");
                Log.Info($"sheetName: {wdbVars.SheetName}");
                Log.Info("");
                Log.Info("");

                wdbVars.FieldCount = (uint)WDBDicts.FieldNames[wdbVars.SheetName].Count;
                wdbVars.Fields = new string[wdbVars.FieldCount];

                // Write all of the field names 
                // if the file is fully known
                for (int sf = 0; sf < wdbVars.FieldCount; sf++)
                {
                    var derivedString = WDBDicts.FieldNames[wdbVars.SheetName][sf];
                    wdbVars.Fields[sf] = derivedString;
                }
            }
            else
            {
                sectionData[JsonVariables.IsKnownToken] = wdbVars.IsKnown;
            }


            // Parse and write the strtypelistData
            List<uint> strtypelistValues = new List<uint>();

            var strtypelistIndex = 0;
            var currentStrtypelistData = new byte[4];
            var strTypelistValueCount = wdbVars.StrtypelistData.Length / 4;
            uint strtypelistValue;

            for (int s = 0; s < strTypelistValueCount; s++)
            {
                Array.ConstrainedCopy(wdbVars.StrtypelistData, strtypelistIndex, currentStrtypelistData, 0, 4);
                Array.Reverse(currentStrtypelistData);
                strtypelistValue = BitConverter.ToUInt32(currentStrtypelistData, 0);

                wdbVars.StrtypelistValues.Add(strtypelistValue);
                strtypelistValues.Add(strtypelistValue);
                strtypelistIndex += 4;
            }
            sectionData[wdbVars.StrtypelistSectionName] = strtypelistValues;


            // Write all the typelist data
            List<int> typelistValues = new List<int>();

            var typelistIndex = 0;
            var currentTypelistData = new byte[4];
            int typelistValue;

            for (int t = 0; t < wdbVars.TypelistData.Length / 4; t++)
            {
                Array.ConstrainedCopy(wdbVars.TypelistData, typelistIndex, currentTypelistData, 0, 4);
                Array.Reverse(currentTypelistData);
                typelistValue = (int)BitConverter.ToUInt32(currentTypelistData, 0);

                typelistValues.Add(typelistValue);
                typelistIndex += 4;
            }
            sectionData[wdbVars.TypelistSectionName] = typelistValues;


            // Write version data
            sectionData[wdbVars.VersionSectionName] = SharedMethods.DeriveUIntFromSectionData(wdbVars.VersionData, 0, true);


            // Write fields data
            if (wdbVars.IsKnown && !wdbVars.IgnoreKnown)
            {
                List<string> fields = new List<string>();

                for (int i = 0; i < wdbVars.FieldCount; i++)
                {
                    fields.Add(wdbVars.Fields[i]);
                }

                sectionData[wdbVars.StructItemSectionName] = fields;
            }

            return sectionData;
        }
    }
}