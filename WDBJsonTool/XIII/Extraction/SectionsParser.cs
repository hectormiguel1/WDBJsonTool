using System.Text.Json;
using WDBJsonTool.Support;
using WDBJsonTool.DataStructures; // Added
using WDBJsonTool; // Added

namespace WDBJsonTool.XIII.Extraction
{
    internal static class SectionsParser
    {
        public static void MainSections(BinaryReader wdbReader, WDBVariablesXIII wdbVars)
        {
            // Parse main sections
            long currentSectionNamePos = 16;

            while (true)
            {
                wdbReader.BaseStream.Position = currentSectionNamePos;
                var sectioNameRead = wdbReader.ReadBytesString(16, false);

                // Break the loop if its
                // not a valid "!" section
                if (!sectioNameRead.StartsWith("!!"))
                {
                    _ = wdbReader.BaseStream.Position = currentSectionNamePos;
                    break;
                }

                switch (sectioNameRead)
                {
                    // !!sheetname check
                    case "!!sheetname":
                    // !!strArray check
                    case "!!strArray":
                        SharedMethods.ErrorExit("Specified WDB file is from XIII-2 or LR. set the gamecode to -ff132 to extract this file.");
                        break;
                    // !!string
                    case WDBVariablesXIII.StringSectionName:
                        wdbVars.HasStringSection = true;

                        wdbVars.StringsData = SharedMethods.SaveSectionData(wdbReader, false);
                        wdbVars.RecordCount--;
                        break;
                    // !!strtypelist
                    case WDBVariablesXIII.StrtypelistSectionName:
                    {
                        wdbVars.StrtypelistData = SharedMethods.SaveSectionData(wdbReader, false);

                        if (wdbVars.StrtypelistData.Length != 0)
                        {
                            wdbVars.StrtypelistValues = SharedMethods.GetSectionDataValues(wdbVars.StrtypelistData);
                            wdbVars.FieldCount = (uint)wdbVars.StrtypelistValues.Count;
                        }

                        wdbVars.RecordCount--;
                        break;
                    }
                    // !!typelist
                    case WDBVariablesXIII.TypelistSectionName:
                    {
                        wdbVars.TypelistData = SharedMethods.SaveSectionData(wdbReader, false);

                        if (wdbVars.TypelistData.Length != 0)
                        {
                            wdbVars.TypelistValues = SharedMethods.GetSectionDataValues(wdbVars.TypelistData);
                        }

                        wdbVars.RecordCount--;
                        break;
                    }
                    // !!version
                    case WDBVariablesXIII.VersionSectionName:
                        wdbVars.VersionData = SharedMethods.SaveSectionData(wdbReader, false);
                        wdbVars.RecordCount--;
                        break;
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
            var sectionData = new WDBSection();

            sectionData[JsonVariables.RecordCountToken] = wdbVars.RecordCount;

            if (WDBDicts.RecordIDs.Any( (entry) => wdbVars.WDBName.Equals(entry.Key)) && !wdbVars.IgnoreKnown)
            {
                wdbVars.IsKnown = true;
                sectionData[JsonVariables.IsKnownToken] = wdbVars.IsKnown;

                wdbVars.SheetName = WDBDicts.RecordIDs.Where((entry) =>  wdbVars.WDBName.Equals(entry.Key)).First().Value;
                sectionData[WDBVariablesXIII.SheetNameSectionName] = wdbVars.SheetName;

                Log.Info($"sheetName: {wdbVars.SheetName} for key: {wdbVars.WDBName}" );

                wdbVars.FieldCount = (uint)WDBDicts.FieldNames[wdbVars.SheetName].Count;
                wdbVars.Fields = new string[wdbVars.FieldCount];

                // Write all of the field names 
                // if the file is fully known
                for (var sf = 0; sf < wdbVars.FieldCount; sf++)
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
            List<uint> strtypelistValues = [];

            var strtypelistIndex = 0;
            var currentStrtypelistData = new byte[4];
            var strTypelistValueCount = wdbVars.StrtypelistData.Length / 4;

            for (var s = 0; s < strTypelistValueCount; s++)
            {
                Array.ConstrainedCopy(wdbVars.StrtypelistData, strtypelistIndex, currentStrtypelistData, 0, 4);
                Array.Reverse(currentStrtypelistData);
                var strtypelistValue = BitConverter.ToUInt32(currentStrtypelistData, 0);

                wdbVars.StrtypelistValues.Add(strtypelistValue);
                strtypelistValues.Add(strtypelistValue);
                strtypelistIndex += 4;
            }
            sectionData[WDBVariablesXIII.StrtypelistSectionName] = strtypelistValues;


            // Write all the typelist data
            List<int> typelistValues = [];

            var typelistIndex = 0;
            var currentTypelistData = new byte[4];

            for (var t = 0; t < wdbVars.TypelistData.Length / 4; t++)
            {
                Array.ConstrainedCopy(wdbVars.TypelistData, typelistIndex, currentTypelistData, 0, 4);
                Array.Reverse(currentTypelistData);
                var typelistValue = (int)BitConverter.ToUInt32(currentTypelistData, 0);

                typelistValues.Add(typelistValue);
                typelistIndex += 4;
            }
            sectionData[WDBVariablesXIII.TypelistSectionName] = typelistValues;


            // Write version data
            sectionData[WDBVariablesXIII.VersionSectionName] = SharedMethods.DeriveUIntFromSectionData(wdbVars.VersionData, 0, true);


            // Write fields data
            if (!wdbVars.IsKnown || wdbVars.IgnoreKnown) return sectionData;
            List<string> fields = [];

            for (var i = 0; i < wdbVars.FieldCount; i++)
            {
                fields.Add(wdbVars.Fields[i]);
            }

            sectionData[WDBVariablesXIII.StructItemSectionName] = fields;

            return sectionData;
        }
    }
}