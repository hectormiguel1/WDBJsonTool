using System.Text.Json;
using WDBJsonTool.Common;
using WDBJsonTool.Common.FieldProcessors;
using WDBJsonTool.Extensions;
using WDBJsonTool.Support;

namespace WDBJsonTool.XIII2LR.Extraction;
internal class RecordsParser
{
    public static void ProcessRecords(BinaryReader wdbReader, WDBVariablesXIII2LR wdbVars, Utf8JsonWriter jsonWriter)
    {
        // Process each record's data
        jsonWriter.WriteStartArray(JsonVariables.RecordsArrayToken);

        var sectionPos = wdbReader.BaseStream.Position;
        var strtypelistIndex = 0;
        var currentRecordDataIndex = 0;

        for (var r = 0; r < wdbVars.RecordCount; r++)
        {
            jsonWriter.WriteStartObject();

            _ = wdbReader.BaseStream.Position = sectionPos;
            var currentRecordName = wdbReader.ReadBytesString(16, false);

            Log.Debug($"Record: {currentRecordName}");
            jsonWriter.WriteString(JsonVariables.RecordToken, currentRecordName);

            var currentRecordData = SharedMethods.SaveSectionData(wdbReader, false);
            for (var f = 0; f < wdbVars.FieldCount; f++)
            {
                switch (wdbVars.StrtypelistValues[strtypelistIndex])
                {
                    // bitpacked
                    case (int)WdbFieldType.Bitpacked:
                        var binaryData = SharedMethods.DeriveUIntFromSectionData(currentRecordData, currentRecordDataIndex, true).UIntToBinary();
                        var binaryDataIndex = binaryData.Length;
                        var fieldBitsToProcess = 32;

                        while (fieldBitsToProcess != 0 && f < wdbVars.FieldCount)
                        {
                            var fieldType = wdbVars.Fields[f].Substring(0, 1);
                            var fieldNum = SharedMethods.DeriveFieldNumber(wdbVars.Fields[f]);

                            switch (fieldType)
                            {
                                // sint
                                case "i":
                                    int iTypedataVal;
                                    if (fieldNum == 0)
                                    {
                                        iTypedataVal = binaryData.BinaryToInt(binaryDataIndex - 32, 32);
                                        fieldBitsToProcess = 0;

                                        Log.Debug($"{wdbVars.Fields[f]}: {iTypedataVal}");
                                        jsonWriter.WriteNumber(wdbVars.Fields[f], iTypedataVal);

                                        break;
                                    }
                                    if (fieldNum > fieldBitsToProcess)
                                    {
                                        f--;
                                        fieldBitsToProcess = 0;
                                        continue;
                                    }

                                    binaryDataIndex -= fieldNum;

                                    iTypedataVal = binaryData.BinaryToInt(binaryDataIndex, fieldNum);
                                    fieldBitsToProcess -= fieldNum;

                                    Log.Debug($"{wdbVars.Fields[f]}: {iTypedataVal}");
                                    jsonWriter.WriteNumber(wdbVars.Fields[f], iTypedataVal);

                                    if (fieldBitsToProcess != 0)
                                    {
                                        f++;
                                    }
                                    break;

                                // uint 
                                case "u":
                                    uint uTypeDataVal;
                                    if (fieldNum == 0)
                                    {
                                        uTypeDataVal = binaryData.BinaryToUInt(binaryDataIndex - 32, 32);
                                        fieldBitsToProcess = 0;

                                        Log.Debug($"{wdbVars.Fields[f]}: {uTypeDataVal}");
                                        jsonWriter.WriteNumber(wdbVars.Fields[f], uTypeDataVal);

                                        break;
                                    }
                                    if (fieldNum > fieldBitsToProcess)
                                    {
                                        f--;
                                        fieldBitsToProcess = 0;
                                        continue;
                                    }

                                    binaryDataIndex -= fieldNum;

                                    uTypeDataVal = binaryData.BinaryToUInt(binaryDataIndex, fieldNum);
                                    fieldBitsToProcess -= fieldNum;

                                    Log.Debug($"{wdbVars.Fields[f]}: {uTypeDataVal}");
                                    jsonWriter.WriteNumber(wdbVars.Fields[f], uTypeDataVal);

                                    if (fieldBitsToProcess != 0)
                                    {
                                        f++;
                                    }
                                    break;

                                // float (bitpacked as int)
                                case "f":
                                    int fTypeDataVal;
                                    if (fieldNum == 0)
                                    {
                                        fTypeDataVal = binaryData.BinaryToInt(binaryDataIndex - 32, 32);
                                        fieldBitsToProcess = 0;

                                        Log.Debug($"{wdbVars.Fields[f]}: {fTypeDataVal}");
                                        jsonWriter.WriteNumber(wdbVars.Fields[f], fTypeDataVal);

                                        break;
                                    }
                                    if (fieldNum > fieldBitsToProcess)
                                    {
                                        f--;
                                        fieldBitsToProcess = 0;
                                        continue;
                                    }

                                    binaryDataIndex -= fieldNum;

                                    fTypeDataVal = binaryData.BinaryToInt(binaryDataIndex, fieldNum);
                                    fieldBitsToProcess -= fieldNum;

                                    Log.Debug($"{wdbVars.Fields[f]}: {fTypeDataVal}");
                                    jsonWriter.WriteNumber(wdbVars.Fields[f], fTypeDataVal);

                                    if (fieldBitsToProcess != 0)
                                    {
                                        f++;
                                    }
                                    break;

                                // (s#) strArray item index
                                case "s":
                                    if (fieldNum > fieldBitsToProcess)
                                    {
                                        f--;
                                        fieldBitsToProcess = 0;
                                        continue;
                                    }

                                    binaryDataIndex -= fieldNum;

                                    var strArrayTypeDataVal = binaryData.BinaryToUInt(binaryDataIndex, fieldNum);
                                    fieldBitsToProcess -= fieldNum;

                                    var strArrayTypeDictKey = wdbVars.Fields[f];
                                    var strArrayTypeDictList = wdbVars.StrArrayDict[strArrayTypeDictKey];

                                    var strArrayTypeStringVal = strArrayTypeDataVal < strArrayTypeDictList.Count ? strArrayTypeDictList[(int)strArrayTypeDataVal] : "";

                                    Log.Debug($"{strArrayTypeDictKey}: {strArrayTypeStringVal}");
                                    jsonWriter.WriteString(strArrayTypeDictKey, strArrayTypeStringVal);

                                    if (fieldBitsToProcess != 0)
                                    {
                                        f++;
                                    }
                                    break;
                            }
                        }

                        strtypelistIndex++;
                        currentRecordDataIndex += 4;
                        break;

                    // float value
                    case (int)WdbFieldType.Float:
                        FieldProcessorHelper.ProcessFloatField(
                            currentRecordData.AsSpan(),
                            currentRecordDataIndex,
                            wdbVars.Fields[f],
                            jsonWriter);

                        strtypelistIndex++;
                        currentRecordDataIndex += 4;
                        break;

                    // !!string section offset
                    case (int)WdbFieldType.String:
                        var stringDataOffset = FieldProcessorHelper.ExtractStringOffset(
                            currentRecordData.AsSpan(),
                            currentRecordDataIndex);
                        var derivedString = SharedMethods.DeriveStringFromArray(wdbVars.StringsData, (int)stringDataOffset);

                        Log.Debug($"{wdbVars.Fields[f]}: {derivedString}");
                        jsonWriter.WriteString(wdbVars.Fields[f], derivedString);

                        strtypelistIndex++;
                        currentRecordDataIndex += 4;
                        break;

                    // uint value
                    case (int)WdbFieldType.UInt:
                        FieldProcessorHelper.ProcessUIntField(
                            currentRecordData.AsSpan(),
                            currentRecordDataIndex,
                            wdbVars.Fields[f],
                            jsonWriter,
                            is64Bit: false);

                        strtypelistIndex++;
                        currentRecordDataIndex += 4;
                        break;
                }
            }

            jsonWriter.WriteEndObject();

            
            strtypelistIndex = 0;
            currentRecordDataIndex = 0;
            sectionPos += 32;
        }

        jsonWriter.WriteEndArray();
    }
}
