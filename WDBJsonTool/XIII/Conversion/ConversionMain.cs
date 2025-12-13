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
                Log.Fine($"{WDBVariablesXIII.SheetNameSectionName}: {wdbVars.SheetName}");
            }

            Log.Fine($"Total records (with sections): {wdbVars.RecordCountWithSections}");

            Log.Fine("Building records....");


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
            
           Log.Finest("Finished building wdb file for extracted json data");
        }
    }
}