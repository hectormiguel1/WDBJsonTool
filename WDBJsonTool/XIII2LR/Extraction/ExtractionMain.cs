using WDBJsonTool.Support;
using WDBJsonTool.DataStructures; // Added
using WDBJsonTool; // Added

namespace WDBJsonTool.XIII2LR.Extraction
{
    internal class ExtractionMain
    {
        public static void StartExtraction(string inWDBfile)
        {
            var wdbVars = new WDBVariablesXIII2LR();
            
            // Instantiate WDBFile
            WDBFile wdbFile = new WDBFile();
            wdbFile.WDBName = Path.GetFileNameWithoutExtension(inWDBfile);

            using (var wdbReader = new BinaryReader(File.Open(inWDBfile, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                // wdbVars.JsonFilePath is no longer directly used for writing in this method.
                // It's passed to JsonWriter.WriteWDBFileToJson

                _ = wdbReader.BaseStream.Position = 0;
                if (wdbReader.ReadBytesString(3, false) != "WPD")
                {
                    SharedMethods.ErrorExit("Not a valid WPD file");
                }

                _ = wdbReader.BaseStream.Position += 1;
                wdbVars.RecordCount = wdbReader.ReadBytesUInt32(true);

                if (wdbVars.RecordCount == 0)
                {
                    SharedMethods.ErrorExit("No records/sections are present in this file");
                }

                SectionsParser.MainSections(wdbReader, wdbVars);

                Log.Info("");
                Log.Info($"Total records: {wdbVars.RecordCount}");
                Log.Info("");

                // Populate WDBFile.Sections from SectionsParser
                wdbFile.Sections[JsonVariables.HeaderSectionToken] = SectionsParser.ParseSectionsToWDBSection(wdbVars);

                Log.Info("Parsing records....");
                Log.Info("");
                Thread.Sleep(1000);

                // Populate WDBFile.Records from RecordsParser
                wdbFile.Records = RecordsParser.ProcessRecords(wdbReader, wdbVars);
                
                // Write the complete WDBFile to JSON
                JsonWriter.WriteWDBFileToJson(wdbFile, Path.Combine(Path.GetDirectoryName(inWDBfile), wdbFile.WDBName + ".json"));
            }

            Log.Info("");
            Log.Info("");
            Log.Info("Finished extracting wdb data to json file");
        }
    }
}