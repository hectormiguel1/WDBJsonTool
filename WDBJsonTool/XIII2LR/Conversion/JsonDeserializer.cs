using System.Text;
using System.Text.Json;
using WDBJsonTool.Support;

namespace WDBJsonTool.XIII2LR.Conversion
{
    internal static class JsonDeserializer
    {
        public static void DeserializeData(string inJsonFile, WDBVariablesXIII2LR wdbVars)
        {
            var jsonData = File.ReadAllBytes(inJsonFile);

            var options = new JsonReaderOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };

            var jsonReader = new Utf8JsonReader(jsonData, options);
            _ = jsonReader.Read();

            Log.Fine("Deserializing main sections....");
            DeserializeMainSections(ref jsonReader, wdbVars);

            Log.Fine("Deserializing records....");
            DeserializeRecords(ref jsonReader, wdbVars);
        }


        private static void DeserializeMainSections(ref Utf8JsonReader jsonReader, WDBVariablesXIII2LR wdbVars)
        {
            // Get recordCount
            JsonMethods.CheckTokenType("PropertyName", ref jsonReader, JsonVariables.RecordCountToken);
            JsonMethods.CheckPropertyName(ref jsonReader, JsonVariables.RecordCountToken);
            JsonMethods.CheckTokenType("Number", ref jsonReader, JsonVariables.RecordCountToken);
            wdbVars.RecordCount = jsonReader.GetUInt32();


            // Get sheetName
            JsonMethods.CheckTokenType("PropertyName", ref jsonReader, wdbVars.SheetNameSectionName);
            JsonMethods.CheckPropertyName(ref jsonReader, wdbVars.SheetNameSectionName);
            JsonMethods.CheckTokenType("String", ref jsonReader, wdbVars.SheetNameSectionName);
            wdbVars.SheetName = jsonReader.GetString();

            if (wdbVars.SheetName != "Not Specified")
            {
                wdbVars.RecordCountWithSections++;

                wdbVars.SheetName += "\0";
                wdbVars.SheetNameData = Encoding.UTF8.GetBytes(wdbVars.SheetName);
            }


            // Check if strArray is
            // present
            JsonMethods.CheckTokenType("PropertyName", ref jsonReader, JsonVariables.HasStrArrayToken);
            JsonMethods.CheckPropertyName(ref jsonReader, JsonVariables.HasStrArrayToken);
            JsonMethods.CheckTokenType("Bool", ref jsonReader, JsonVariables.HasStrArrayToken);
            wdbVars.HasStrArraySection = jsonReader.GetBoolean();

            if (wdbVars.HasStrArraySection)
            {
                wdbVars.RecordCountWithSections += 3;

                // Get strArrayInfo values
                JsonMethods.CheckTokenType("PropertyName", ref jsonReader, JsonVariables.BitsPerOffsetToken);
                JsonMethods.CheckPropertyName(ref jsonReader, JsonVariables.BitsPerOffsetToken);
                JsonMethods.CheckTokenType("Number", ref jsonReader, JsonVariables.BitsPerOffsetToken);
                wdbVars.BitsPerOffset = jsonReader.GetByte();

                JsonMethods.CheckTokenType("PropertyName", ref jsonReader, JsonVariables.OffsetsPerValueToken);
                JsonMethods.CheckPropertyName(ref jsonReader, JsonVariables.OffsetsPerValueToken);
                JsonMethods.CheckTokenType("Number", ref jsonReader, JsonVariables.OffsetsPerValueToken);
                wdbVars.OffsetsPerValue = jsonReader.GetByte();

                wdbVars.StrArrayInfoData = new byte[4];
                wdbVars.StrArrayInfoData[2] = wdbVars.OffsetsPerValue;
                wdbVars.StrArrayInfoData[3] = wdbVars.BitsPerOffset;
            }


            // Check and determine how to parse
            // strtypelist
            JsonMethods.CheckTokenType("PropertyName", ref jsonReader, JsonVariables.IsStrTypelistV1Token);
            JsonMethods.CheckPropertyName(ref jsonReader, JsonVariables.IsStrTypelistV1Token);
            JsonMethods.CheckTokenType("Bool", ref jsonReader, JsonVariables.IsStrTypelistV1Token);
            wdbVars.ParseStrtypelistAsV1 = jsonReader.GetBoolean();

            var strtypelistSecNameProcess = wdbVars.ParseStrtypelistAsV1 ? wdbVars.StrtypelistSectionName : wdbVars.StrtypelistbSectionName;
            JsonMethods.CheckTokenType("PropertyName", ref jsonReader, strtypelistSecNameProcess);
            JsonMethods.CheckPropertyName(ref jsonReader, strtypelistSecNameProcess);

            wdbVars.RecordCountWithSections++;


            // Get strtypelist values
            JsonMethods.CheckTokenType("Array", ref jsonReader, strtypelistSecNameProcess);
            wdbVars.StrtypelistValues = JsonMethods.GetNumbersFromArrayPropertyInt(ref jsonReader, strtypelistSecNameProcess);

            if (wdbVars.ParseStrtypelistAsV1)
            {
                wdbVars.StrtypelistData = new byte[wdbVars.StrtypelistValues.Count * 4];
                wdbVars.StrtypelistData = SharedMethods.CreateArrayFromIntList(wdbVars.StrtypelistValues, 4);
            }
            else
            {
                wdbVars.StrtypelistData = new byte[wdbVars.StrtypelistValues.Count];
                wdbVars.StrtypelistData = SharedMethods.CreateArrayFromIntList(wdbVars.StrtypelistValues, 1);
            }


            // Check if typelist is
            // present 
            JsonMethods.CheckTokenType("PropertyName", ref jsonReader, JsonVariables.HasTypelistToken);
            JsonMethods.CheckPropertyName(ref jsonReader, JsonVariables.HasTypelistToken);
            JsonMethods.CheckTokenType("Bool", ref jsonReader, JsonVariables.HasTypelistToken);
            wdbVars.HasTypelistSection = jsonReader.GetBoolean();

            if (wdbVars.HasTypelistSection)
            {
                wdbVars.RecordCountWithSections++;

                // Get typelist values
                JsonMethods.CheckTokenType("PropertyName", ref jsonReader, wdbVars.TypelistSectionName);
                JsonMethods.CheckTokenType("Array", ref jsonReader, wdbVars.TypelistSectionName);
                var typelistValues = JsonMethods.GetNumbersFromArrayPropertyInt(ref jsonReader, wdbVars.TypelistSectionName);

                wdbVars.TypelistData = new byte[typelistValues.Count * 4];
                wdbVars.TypelistData = SharedMethods.CreateArrayFromIntList(typelistValues, 4);
            }


            // Get version
            JsonMethods.CheckTokenType("PropertyName", ref jsonReader, wdbVars.VersionSectionName);
            JsonMethods.CheckPropertyName(ref jsonReader, wdbVars.VersionSectionName);
            JsonMethods.CheckTokenType("Number", ref jsonReader, wdbVars.VersionSectionName);
            wdbVars.VersionData = BitConverter.GetBytes(jsonReader.GetUInt32());
            Array.Reverse(wdbVars.VersionData);

            wdbVars.RecordCountWithSections++;


            // Get structitem values
            JsonMethods.CheckTokenType("PropertyName", ref jsonReader, wdbVars.StructItemSectionName);
            JsonMethods.CheckPropertyName(ref jsonReader, wdbVars.StructItemSectionName);
            JsonMethods.CheckTokenType("Array", ref jsonReader, wdbVars.StructItemSectionName);
            wdbVars.RecordCountWithSections += 2;

            wdbVars.Fields = JsonMethods.GetStringsFromArrayProperty(ref jsonReader, wdbVars.StructItemSectionName).ToArray();
            wdbVars.FieldCount = (uint)wdbVars.Fields.Length;
            var structItemsList = new List<byte>();

            for (var i = 0; i < wdbVars.FieldCount; i++)
            {
                structItemsList.AddRange(Encoding.UTF8.GetBytes(wdbVars.Fields[i] + "\0"));
            }

            wdbVars.StructItemData = structItemsList.ToArray();
            wdbVars.StructItemNumData = BitConverter.GetBytes(wdbVars.FieldCount);
            Array.Reverse(wdbVars.StructItemNumData);

            wdbVars.RecordCountWithSections += wdbVars.RecordCount;


            // Determine whether there is
            // a string section
            if (wdbVars.HasStringSection) return;
            if (!wdbVars.StrtypelistValues.Contains(2) && !wdbVars.HasStrArraySection) return;
            wdbVars.HasStringSection = true;
            wdbVars.RecordCountWithSections++;
        }


        private static void DeserializeRecords(ref Utf8JsonReader jsonReader, WDBVariablesXIII2LR wdbVars)
        {
            // Get record values
            JsonMethods.CheckTokenType("PropertyName", ref jsonReader, JsonVariables.RecordsArrayToken);
            JsonMethods.CheckPropertyName(ref jsonReader, JsonVariables.RecordsArrayToken);
            JsonMethods.CheckTokenType("Array", ref jsonReader, JsonVariables.RecordsArrayToken);

            var recordName = string.Empty;

            for (var i = 0; i < wdbVars.RecordCount; i++)
            {
                // Read start object
                _ = jsonReader.Read();

                if (jsonReader.TokenType != JsonTokenType.StartObject)
                {
                    SharedMethods.ErrorExit(recordName == ""
                        ? "The record array does not begin with a valid start object character"
                        : $"A valid start object character was not present after this record {recordName}");
                }

                // Get record name
                _ = jsonReader.Read();
                _ = jsonReader.Read();

                if (jsonReader.TokenType != JsonTokenType.String)
                {
                    SharedMethods.ErrorExit(recordName == ""
                        ? "The first record's property value does not begin with a valid name"
                        : $"Invalid property value specified for 'record' property. previous record read was {recordName}");
                }

                recordName = jsonReader.GetString();
                var currentDataList = new List<object>();

                // Get record data
                for (var f = 0; f < wdbVars.FieldCount; f++)
                {
                    _ = jsonReader.Read();

                    if (jsonReader.TokenType != JsonTokenType.PropertyName)
                    {
                        SharedMethods.ErrorExit($"Field name PropertyType was invalid. occured when parsing {recordName} data.");
                    }

                    var fieldName = jsonReader.GetString();

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
}