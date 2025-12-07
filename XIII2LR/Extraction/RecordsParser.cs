using System.Text.Json;
using WDBJsonTool.Support;
using WDBJsonTool.Extensions;

using WDBJsonTool.Common;
namespace WDBJsonTool.XIII2LR.Extraction;
internal class RecordsParser
{
    public static void ProcessRecords(BinaryReader wdbReader, WDBVariablesXIII2LR wdbVars, Utf8JsonWriter jsonWriter)
    {
        // Process each record's data
        jsonWriter.WriteStartArray(JsonVariables.RecordsArrayToken);

        var sectionPos = wdbReader.BaseStream.Position;
        string currentRecordName;
        byte[] currentRecordData;
        var strtypelistIndex = 0;
        var currentRecordDataIndex = 0;

        for (int r = 0; r < wdbVars.RecordCount; r++)
        {
            jsonWriter.WriteStartObject();

            _ = wdbReader.BaseStream.Position = sectionPos;
            currentRecordName = wdbReader.ReadBytesString(16, false);

            Log.Debug($"Record: {currentRecordName}");
            jsonWriter.WriteString(JsonVariables.RecordToken, currentRecordName);

            currentRecordData = SharedMethods.SaveSectionData(wdbReader, false);
            for (int f = 0; f < wdbVars.FieldCount; f++)
            {
                switch (wdbVars.StrtypelistValues[strtypelistIndex])
                {
                    // bitpacked
                    case (int)WdbFieldType.Bitpacked:
                        var binaryData = BitOperationHelpers.UIntToBinary(SharedMethods.DeriveUIntFromSectionData(currentRecordData, currentRecordDataIndex, true));
                        var binaryDataIndex = binaryData.Length;
                        var fieldBitsToProcess = 32;

                        int iTypedataVal;
                        uint uTypeDataVal;
                        int fTypeDataVal;
                        uint strArrayTypeDataVal;
                        string strArrayTypeDictKey;
                        List<string> strArrayTypeDictList;
                        string strArrayTypeStringVal;

                        while (fieldBitsToProcess != 0 && f < wdbVars.FieldCount)
                        {
                            var fieldType = wdbVars.Fields[f].Substring(0, 1);
                            var fieldNum = SharedMethods.DeriveFieldNumber(wdbVars.Fields[f]);

                            switch (fieldType)
                            {
                                // sint
                                case "i":
                                    if (fieldNum == 0)
                                    {
                                        iTypedataVal = BitOperationHelpers.BinaryToInt(binaryData, binaryDataIndex - 32, 32);
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
                                    else
                                    {
                                        binaryDataIndex -= fieldNum;

                                        iTypedataVal = BitOperationHelpers.BinaryToInt(binaryData, binaryDataIndex, fieldNum);
                                        fieldBitsToProcess -= fieldNum;

                                        Log.Debug($"{wdbVars.Fields[f]}: {iTypedataVal}");
                                        jsonWriter.WriteNumber(wdbVars.Fields[f], iTypedataVal);

                                        if (fieldBitsToProcess != 0)
                                        {
                                            f++;
                                        }
                                    }
                                    break;

                                // uint 
                                case "u":
                                    if (fieldNum == 0)
                                    {
                                        uTypeDataVal = BitOperationHelpers.BinaryToUInt(binaryData, binaryDataIndex - 32, 32);
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
                                    else
                                    {
                                        binaryDataIndex -= fieldNum;

                                        uTypeDataVal = BitOperationHelpers.BinaryToUInt(binaryData, binaryDataIndex, fieldNum);
                                        fieldBitsToProcess -= fieldNum;

                                        Log.Debug($"{wdbVars.Fields[f]}: {uTypeDataVal}");
                                        jsonWriter.WriteNumber(wdbVars.Fields[f], uTypeDataVal);

                                        if (fieldBitsToProcess != 0)
                                        {
                                            f++;
                                        }
                                    }
                                    break;

                                // float (bitpacked as int)
                                case "f":
                                    if (fieldNum == 0)
                                    {
                                        fTypeDataVal = BitOperationHelpers.BinaryToInt(binaryData, binaryDataIndex - 32, 32);
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
                                    else
                                    {
                                        binaryDataIndex -= fieldNum;

                                        fTypeDataVal = BitOperationHelpers.BinaryToInt(binaryData, binaryDataIndex, fieldNum);
                                        fieldBitsToProcess -= fieldNum;

                                        Log.Debug($"{wdbVars.Fields[f]}: {fTypeDataVal}");
                                        jsonWriter.WriteNumber(wdbVars.Fields[f], fTypeDataVal);

                                        if (fieldBitsToProcess != 0)
                                        {
                                            f++;
                                        }
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
                                    else
                                    {
                                        binaryDataIndex -= fieldNum;

                                        strArrayTypeDataVal = BitOperationHelpers.BinaryToUInt(binaryData, binaryDataIndex, fieldNum);
                                        fieldBitsToProcess -= fieldNum;

                                        strArrayTypeDictKey = wdbVars.Fields[f];
                                        strArrayTypeDictList = wdbVars.StrArrayDict[strArrayTypeDictKey];

                                        if (strArrayTypeDataVal < strArrayTypeDictList.Count)
                                        {
                                            strArrayTypeStringVal = strArrayTypeDictList[(int)strArrayTypeDataVal];
                                        }
                                        else
                                        {
                                            strArrayTypeStringVal = "";
                                        }

                                        Log.Debug($"{strArrayTypeDictKey}: {strArrayTypeStringVal}");
                                        jsonWriter.WriteString(strArrayTypeDictKey, strArrayTypeStringVal);

                                        if (fieldBitsToProcess != 0)
                                        {
                                            f++;
                                        }
                                    }
                                    break;
                            }
                        }

                        strtypelistIndex++;
                        currentRecordDataIndex += 4;
                        break;

                    // float value
                    case (int)WdbFieldType.Float:
                        var floatDataVal = SharedMethods.DeriveFloatFromSectionData(currentRecordData, currentRecordDataIndex, true);

                        Log.Debug($"{wdbVars.Fields[f]}: {floatDataVal}");
                        jsonWriter.WriteNumber(wdbVars.Fields[f], floatDataVal);

                        strtypelistIndex++;
                        currentRecordDataIndex += 4;
                        break;

                    // !!string section offset
                    case (int)WdbFieldType.String:
                        var stringDataOffset = SharedMethods.DeriveUIntFromSectionData(currentRecordData, currentRecordDataIndex, true);
                        var derivedString = SharedMethods.DeriveStringFromArray(wdbVars.StringsData, (int)stringDataOffset);

                        Log.Debug($"{wdbVars.Fields[f]}: {derivedString}");
                        jsonWriter.WriteString(wdbVars.Fields[f], derivedString);

                        strtypelistIndex++;
                        currentRecordDataIndex += 4;
                        break;

                    // uint value
                    case (int)WdbFieldType.UInt:
                        var uintDataVal = SharedMethods.DeriveUIntFromSectionData(currentRecordData, currentRecordDataIndex, true);

                        Log.Debug($"{wdbVars.Fields[f]}: {uintDataVal}");
                        jsonWriter.WriteNumber(wdbVars.Fields[f], uintDataVal);

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
