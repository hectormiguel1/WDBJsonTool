namespace WDBJsonTool.XIII2LR.Conversion
{
    internal class ConversionMain
    {
        public static void StartConversion(string inJsonFile)
        {
            var wdbVars = new WDBVariablesXIII2LR();

            JsonDeserializer.DeserializeData(inJsonFile, wdbVars);

            Log.Info($"{wdbVars.SheetNameSectionName}: {wdbVars.SheetName}");
            Log.Info($"Total records (with sections): {wdbVars.RecordCountWithSections}");

            wdbVars.WDBFilePath = Path.Combine(Path.GetDirectoryName(inJsonFile), Path.GetFileNameWithoutExtension(inJsonFile) + ".wdb");

            if (wdbVars.HasStrArraySection)
            {
                RecordsConversion.ConvertRecordsStrArray(wdbVars);
            }
            else
            {
                RecordsConversion.ConvertRecords(wdbVars);
            }

            Log.Info("Building wdb file....");

            WDBbuilder.BuildWDB(wdbVars);

            Log.Info("Finished building wdb file for extracted json data");
        }
    }
}