using System.Text;
using System.Text.Json;
using WDBJsonTool.Support;
using WDBJsonTool.Extensions;

namespace WDBJsonTool.XIII2LR.Extraction;
internal static class SectionsParser
{
    public static void MainSections(BinaryReader wdbReader, WDBVariablesXIII2LR wdbVars)
    {
        // Parse main sections
        long currentSectionNamePos = 16;

        wdbVars.StrtypelistData = [];
        wdbVars.StructItemData = [];
        wdbVars.FieldCount = 0;


        while (true)
        {
            wdbReader.BaseStream.Position = currentSectionNamePos;
            var sectioNameRead = wdbReader.ReadBytesString(16, false);

            // Break the loop if its
            // not a valid "!" section
            if (!sectioNameRead.StartsWith("!"))
            {
                _ = wdbReader.BaseStream.Position = currentSectionNamePos;
                break;
            }

            // !!sheetname
            if (sectioNameRead == WDBVariablesXIII2LR.SheetNameSectionName)
            {
                _ = wdbReader.BaseStream.Position = wdbReader.ReadBytesUInt32(true);
                wdbVars.SheetName = wdbReader.ReadStringTillNull();
                wdbVars.RecordCount--;
            }

            // !!strArray
            if (sectioNameRead == WDBVariablesXIII2LR.StrArraySectionName)
            {
                wdbVars.HasStrArraySection = true;

                _ = wdbReader.BaseStream.Position = currentSectionNamePos;
                StrArrayParser.SubSections(wdbReader, wdbVars);
            }

            // !!string
            if (sectioNameRead == WDBVariablesXIII2LR.StringSectionName)
            {
                wdbVars.HasStringSection = true;

                wdbVars.StringsData = SharedMethods.SaveSectionData(wdbReader, false);
                wdbVars.RecordCount--;
            }

            // !!strtypelist
            if (sectioNameRead == WDBVariablesXIII2LR.StrtypelistSectionName)
            {
                wdbVars.ParseStrtypelistAsV1 = true;
                wdbVars.StrtypelistData = SharedMethods.SaveSectionData(wdbReader, false);
                wdbVars.RecordCount--;
            }

            // !!strtypelistb
            if (sectioNameRead == WDBVariablesXIII2LR.StrtypelistbSectionName)
            {
                wdbVars.ParseStrtypelistAsV1 = false;
                wdbVars.StrtypelistData = SharedMethods.SaveSectionData(wdbReader, false);
                wdbVars.RecordCount--;
            }

            // !!typelist
            if (sectioNameRead == WDBVariablesXIII2LR.TypelistSectionName)
            {
                wdbVars.HasTypelistSection = true;
                wdbVars.TypelistData = SharedMethods.SaveSectionData(wdbReader, false);
                wdbVars.RecordCount--;
            }

            // !!version
            if (sectioNameRead == WDBVariablesXIII2LR.VersionSectionName)
            {
                wdbVars.VersionData = SharedMethods.SaveSectionData(wdbReader, false);
                wdbVars.RecordCount--;
            }

            // !structitem
            if (sectioNameRead == WDBVariablesXIII2LR.StructItemSectionName)
            {
                wdbVars.StructItemData = SharedMethods.SaveSectionData(wdbReader, false);
                wdbVars.RecordCount--;
            }

            // !structitemnum
            if (sectioNameRead == WDBVariablesXIII2LR.StructItemNumSectionName)
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

        if (string.IsNullOrEmpty(wdbVars.SheetName))
        {
            wdbVars.SheetName = "Not Specified";
        }

        Log.Info($"{WDBVariablesXIII2LR.SheetNameSectionName}: {wdbVars.SheetName}");

        // Process !structitem data
        wdbVars.Fields = new string[wdbVars.FieldCount];
        var stringStartPos = 0;

        var sf = 0;
        for (; sf < wdbVars.FieldCount; sf++)
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
        if (!wdbVars.HasStrArraySection) return;
        Log.Info($"Organizing {WDBVariablesXIII2LR.StrArraySectionName} data....");

        StrArrayParser.ArrangeArrayData(wdbVars);
    }


    public static void MainSectionsToJson(WDBVariablesXIII2LR wdbVars, Utf8JsonWriter jsonWriter)
    {
        jsonWriter.WriteNumber(JsonVariables.RecordCountToken, wdbVars.RecordCount);
        jsonWriter.WriteString(WDBVariablesXIII2LR.SheetNameSectionName, wdbVars.SheetName);
        jsonWriter.WriteBoolean(JsonVariables.HasStrArrayToken, wdbVars.HasStrArraySection);


        // Write array info values
        // if strArray section is
        // present
        if (wdbVars.HasStrArraySection)
        {
            jsonWriter.WriteNumber(JsonVariables.BitsPerOffsetToken, wdbVars.BitsPerOffset);
            jsonWriter.WriteNumber(JsonVariables.OffsetsPerValueToken, wdbVars.OffsetsPerValue);
        }


        // Parse and write the strtypelistData
        jsonWriter.WriteBoolean(JsonVariables.IsStrTypelistV1Token, wdbVars.ParseStrtypelistAsV1);

        jsonWriter.WriteStartArray(wdbVars.ParseStrtypelistAsV1
            ? WDBVariablesXIII2LR.StrtypelistSectionName
            : WDBVariablesXIII2LR.StrtypelistbSectionName);

        var strtypelistbIndex = 0;
        var currentStrtypelistData = new byte[4];
        var strtypelistIndexAdjust = wdbVars.ParseStrtypelistAsV1 ? 4 : 1;
        var strTypelistValueCount = wdbVars.ParseStrtypelistAsV1 ? wdbVars.StrtypelistData.Length / 4 : wdbVars.StrtypelistData.Length;

        for (var s = 0; s < strTypelistValueCount; s++)
        {
            int strtypelistValue;
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

            wdbVars.StrtypelistValues.Add(strtypelistValue);
            jsonWriter.WriteNumberValue(strtypelistValue);
            strtypelistbIndex += strtypelistIndexAdjust;
        }

        jsonWriter.WriteEndArray();


        // Write all the typelist data
        jsonWriter.WriteBoolean(JsonVariables.HasTypelistToken, wdbVars.HasTypelistSection);
        if (wdbVars.HasTypelistSection)
        {
            jsonWriter.WriteStartArray(WDBVariablesXIII2LR.TypelistSectionName);

            var typelistbIndex = 0;
            var currentTypelistData = new byte[4];

            for (var t = 0; t < wdbVars.TypelistData.Length / 4; t++)
            {
                Array.ConstrainedCopy(wdbVars.TypelistData, typelistbIndex, currentTypelistData, 0, 4);
                Array.Reverse(currentTypelistData);
                var typelistValue = (int)BitConverter.ToUInt32(currentTypelistData, 0);

                jsonWriter.WriteNumberValue(typelistValue);
                typelistbIndex += 4;
            }

            jsonWriter.WriteEndArray();
        }


        // Write version data
        jsonWriter.WriteNumber(WDBVariablesXIII2LR.VersionSectionName, SharedMethods.DeriveUIntFromSectionData(wdbVars.VersionData, 0, true));


        // Write structitem data
        jsonWriter.WriteStartArray(WDBVariablesXIII2LR.StructItemSectionName);

        for (var i = 0; i < wdbVars.FieldCount; i++)
        {
            jsonWriter.WriteStringValue(wdbVars.Fields[i]);
        }

        jsonWriter.WriteEndArray();
    }
}
