using WDBJsonTool.Support;
using WDBJsonTool.DataStructures;
using WDBJsonTool;

namespace WDBJsonTool.XIII2LR.Extraction
{
    internal class ExtractionMain
    {
        public static WDBFile StartExtraction(string inWDBfile)
        {
            var wdbVars = new WDBVariablesXIII2LR();
            
            WDBFile wdbFile = new WDBFile();
            wdbFile.WDBName = Path.GetFileNameWithoutExtension(inWDBfile);

            using (var wdbReader = new BinaryReader(File.Open(inWDBfile, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
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

                wdbFile.Sections[JsonVariables.HeaderSectionToken] = SectionsParser.ParseSectionsToWDBSection(wdbVars);

                Log.Info("Parsing records....");
                Log.Info("");
                Thread.Sleep(1000);

                wdbFile.Records = RecordsParser.ProcessRecords(wdbReader, wdbVars);
            }

            Log.Info("");
            Log.Info("");
            Log.Info("Finished extracting wdb data to json file"); // Keep this log for now, can be removed later if it's confusing.
            return wdbFile;
        }
    }
}