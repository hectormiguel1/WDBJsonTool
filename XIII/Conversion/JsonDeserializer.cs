using System.Text.Json;
using WDBJsonTool.Support;

namespace WDBJsonTool.XIII.Conversion;
internal static class JsonDeserializer
{
    public static void DeserializeData(string inJsonFile, WDBVariablesXIII wdbVars)
    {
        var jsonData = File.ReadAllBytes(inJsonFile);

        var options = new JsonReaderOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        var jsonReader = new Utf8JsonReader(jsonData, options);
        _ = jsonReader.Read();

        Log.Info("Deserializing main sections....");
        DeserializeMainSections(ref jsonReader, wdbVars);

        Log.Info("Deserializing records....");
        DeserializeRecords(ref jsonReader, wdbVars);
    }


    private static void DeserializeMainSections(ref Utf8JsonReader jsonReader, WDBVariablesXIII wdbVars)
    {
        // Get recordCount
        JsonMethods.CheckTokenType("PropertyName", ref jsonReader, JsonVariables.RecordCountToken);
        JsonMethods.CheckPropertyName(ref jsonReader, JsonVariables.RecordCountToken);
        JsonMethods.CheckTokenType("Number", ref jsonReader, JsonVariables.RecordCountToken);
        wdbVars.RecordCount = jsonReader.GetUInt32();


        // Determine if the file
        // is known
        JsonMethods.CheckTokenType("PropertyName", ref jsonReader, JsonVariables.IsKnownToken);
        JsonMethods.CheckPropertyName(ref jsonReader, JsonVariables.IsKnownToken);
        JsonMethods.CheckTokenType("Bool", ref jsonReader, JsonVariables.IsKnownToken);
        wdbVars.IsKnown = jsonReader.GetBoolean();


        // Get the sheetname if the 
        // file is known
        if (wdbVars.IsKnown)
        {
            // Get sheetName
            JsonMethods.CheckTokenType("PropertyName", ref jsonReader, WDBVariablesXIII.SheetNameSectionName);
            JsonMethods.CheckPropertyName(ref jsonReader, WDBVariablesXIII.SheetNameSectionName);
            JsonMethods.CheckTokenType("String", ref jsonReader, WDBVariablesXIII.SheetNameSectionName);
        }


        // Get the strtypelists 
        // values
        JsonMethods.CheckTokenType("PropertyName", ref jsonReader, WDBVariablesXIII.StrtypelistSectionName);
        JsonMethods.CheckPropertyName(ref jsonReader, WDBVariablesXIII.StrtypelistSectionName);
        JsonMethods.CheckTokenType("Array", ref jsonReader, WDBVariablesXIII.StrtypelistSectionName);
        wdbVars.StrtypelistValues = JsonMethods.GetNumbersFromArrayPropertyUInt(ref jsonReader, WDBVariablesXIII.StrtypelistSectionName);

        if (!wdbVars.IsKnown)
        {
            wdbVars.FieldCount = (uint)wdbVars.StrtypelistValues.Count;
        }

        wdbVars.StrtypelistData = new byte[wdbVars.StrtypelistValues.Count * 4];
        wdbVars.StrtypelistData = SharedMethods.CreateArrayFromUIntList(wdbVars.StrtypelistValues);

        wdbVars.RecordCountWithSections++;


        // Get the typelist
        // values
        JsonMethods.CheckTokenType("PropertyName", ref jsonReader, WDBVariablesXIII.TypelistSectionName);
        JsonMethods.CheckPropertyName(ref jsonReader, WDBVariablesXIII.TypelistSectionName);
        JsonMethods.CheckTokenType("Array", ref jsonReader, WDBVariablesXIII.TypelistSectionName);
        wdbVars.TypelistValues = JsonMethods.GetNumbersFromArrayPropertyUInt(ref jsonReader, WDBVariablesXIII.TypelistSectionName);

        wdbVars.TypelistData = new byte[wdbVars.TypelistValues.Count * 4];
        wdbVars.TypelistData = SharedMethods.CreateArrayFromUIntList(wdbVars.TypelistValues);

        wdbVars.RecordCountWithSections++;


        // Get version
        JsonMethods.CheckTokenType("PropertyName", ref jsonReader, WDBVariablesXIII.VersionSectionName);
        JsonMethods.CheckPropertyName(ref jsonReader, WDBVariablesXIII.VersionSectionName);
        JsonMethods.CheckTokenType("Number", ref jsonReader, WDBVariablesXIII.VersionSectionName);
        wdbVars.VersionData = BitConverter.GetBytes(jsonReader.GetUInt32());
        Array.Reverse(wdbVars.VersionData);

        wdbVars.RecordCountWithSections++;

        wdbVars.RecordCountWithSections += wdbVars.RecordCount;


        // Get the structitem values
        // if the file is known
        if (wdbVars.IsKnown)
        {
            JsonMethods.CheckTokenType("PropertyName", ref jsonReader, WDBVariablesXIII.StructItemSectionName);
            JsonMethods.CheckPropertyName(ref jsonReader, WDBVariablesXIII.StructItemSectionName);
            JsonMethods.CheckTokenType("Array", ref jsonReader, WDBVariablesXIII.StructItemSectionName);

            wdbVars.Fields = JsonMethods.GetStringsFromArrayProperty(ref jsonReader, WDBVariablesXIII.StructItemSectionName).ToArray();
            wdbVars.FieldCount = (uint)wdbVars.Fields.Length;
        }


        // Determine whether there is
        // a string section
        wdbVars.RecordCountWithSections++;

        if (!wdbVars.HasStringSection)
        {
            if (wdbVars.StrtypelistValues.Contains(2))
            {
                wdbVars.HasStringSection = true;
            }
        }
    }


    private static void DeserializeRecords(ref Utf8JsonReader jsonReader, WDBVariablesXIII wdbVars)
    {
        // Get record values
        JsonMethods.CheckTokenType("PropertyName", ref jsonReader, JsonVariables.RecordsArrayToken);
        JsonMethods.CheckTokenType("Array", ref jsonReader, JsonVariables.RecordsArrayToken);

        var recordName = string.Empty;

        for (var i = 0; i < wdbVars.RecordCount; i++)
        {
            // Read start object
            _ = jsonReader.Read();

            if (jsonReader.TokenType != JsonTokenType.StartObject)
            {
                if (recordName == "")
                {
                    SharedMethods.ErrorExit("The record array does not begin with a valid start object character");
                }
                else
                {
                    SharedMethods.ErrorExit($"A valid start object character was not present after this record {recordName}");
                }
            }

            // Get record name
            _ = jsonReader.Read();
            _ = jsonReader.Read();

            if (jsonReader.TokenType != JsonTokenType.String)
            {
                if (recordName == "")
                {
                    SharedMethods.ErrorExit("The first record's property value does not begin with a valid name");
                }
                else
                {
                    SharedMethods.ErrorExit($"Invalid property value specified for 'record' property. previous record read was {recordName}");
                }
            }

            recordName = jsonReader.GetString();
            var currentDataList = new List<object>();

            // Get record data
            string fieldName;
            if (wdbVars.IsKnown)
            {
                for (var f = 0; f < wdbVars.FieldCount; f++)
                {
                    _ = jsonReader.Read();

                    if (jsonReader.TokenType != JsonTokenType.PropertyName)
                    {
                        SharedMethods.ErrorExit($"Field name PropertyType was invalid. occured when parsing {recordName} data.");
                    }

                    fieldName = jsonReader.GetString();

                    if (fieldName.StartsWith("s"))
                    {
                        _ = jsonReader.Read();

                        if (jsonReader.TokenType != JsonTokenType.String)
                        {
                            SharedMethods.ErrorExit($"{fieldName} property's value was invalid. occured when parsing {recordName} data.");
                        }

                        currentDataList.Add(jsonReader.GetString());
                    }                       
                    else
                    {
                        _ = jsonReader.Read();

                        if (jsonReader.TokenType != JsonTokenType.Number)
                        {
                            SharedMethods.ErrorExit($"{fieldName} property's value was invalid. occured when parsing {recordName} data.");
                        }

                        currentDataList.Add(jsonReader.GetDecimal());
                    }
                }
            }
            else
            {
                for (var f = 0; f < wdbVars.FieldCount; f++)
                {
                    _ = jsonReader.Read();

                    if (jsonReader.TokenType != JsonTokenType.PropertyName)
                    {
                        SharedMethods.ErrorExit($"Field name PropertyType was invalid. occured when parsing {recordName} data.");
                    }

                    fieldName = jsonReader.GetString();
                    _ = jsonReader.Read();

                    switch (wdbVars.StrtypelistValues[f])
                    {
                        case 0:
                            if (jsonReader.TokenType != JsonTokenType.String)
                            {
                                SharedMethods.ErrorExit($"{fieldName} property's value was invalid. occured when parsing {recordName} data.");
                            }

                            currentDataList.Add(jsonReader.GetString());
                            break;

                        case 1:
                            if (jsonReader.TokenType != JsonTokenType.Number)
                            {
                                SharedMethods.ErrorExit($"{fieldName} property's value was invalid. occured when parsing {recordName} data.");
                            }

                            currentDataList.Add(jsonReader.GetSingle());
                            break;

                        case 2:
                            if (jsonReader.TokenType != JsonTokenType.String)
                            {
                                SharedMethods.ErrorExit($"{fieldName} property's value was invalid. occured when parsing {recordName} data.");
                            }

                            currentDataList.Add(jsonReader.GetString());
                            break;

                        case 3:
                            if (jsonReader.TokenType != JsonTokenType.Number)
                            {
                                SharedMethods.ErrorExit($"{fieldName} property's value was invalid. occured when parsing {recordName} data.");
                            }

                            currentDataList.Add(jsonReader.GetUInt32());
                            break;
                    }
                }
            }

            wdbVars.RecordsDataDict.Add(recordName, currentDataList);

            // Read end object
            _ = jsonReader.Read();

            if (jsonReader.TokenType != JsonTokenType.EndObject)
            {
                SharedMethods.ErrorExit($"A valid end object character was not present after this record {recordName}");
            }
        }
    }
}
