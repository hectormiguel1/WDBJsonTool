namespace WDBJsonTool.XIII.Conversion
{
    internal class ConversionMain
    {
        public static void StartConversion(string inJsonFile)
        {
            var wdbVars = new WDBVariablesXIII();

            JsonDeserializer.DeserializeData(inJsonFile, wdbVars);

            if (wdbVars.IsKnown)
            {
                Log.Info($"{wdbVars.SheetNameSectionName}: {wdbVars.SheetName}");
            }

            Log.Info($"Total records (with sections): {wdbVars.RecordCountWithSections}");

            Log.Info("Building records....");

            wdbVars.WDBFilePath = Path.Combine(Path.GetDirectoryName(inJsonFile), Path.GetFileNameWithoutExtension(inJsonFile) + ".wdb");

            if (wdbVars.IsKnown)
            {
                RecordsConversion.ConvertRecordsWithFields(wdbVars);
            }
            else
            {
                RecordsConversion.ConvertRecordsNoFields(wdbVars);
            }

            WDBbuilder.BuildWDB(wdbVars);

            Log.Info("Finished building wdb file for extracted json data");
        }
    }
}