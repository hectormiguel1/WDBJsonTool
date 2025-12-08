using System.Text.Json;
using WDBJsonTool.Support;
using WDBJsonTool.DataStructures;
using WDBJsonTool;

namespace WDBJsonTool.XIII2LR.Extraction
{
    internal class RecordsParser
    {
        public static List<WDBRecord> ProcessRecords(BinaryReader wdbReader, WDBVariablesXIII2LR wdbVars)
        {
            List<WDBRecord> records = new List<WDBRecord>();

            var sectionPos = wdbReader.BaseStream.Position;
            string currentRecordName;
            byte[] currentRecordData;
            var strtypelistIndex = 0;
            var currentRecordDataIndex = 0;

            for (int r = 0; r < wdbVars.RecordCount; r++)
            {
                WDBRecord currentRecord = new WDBRecord();

                _ = wdbReader.BaseStream.Position = sectionPos;
                currentRecordName = wdbReader.ReadBytesString(16, false);

                Log.Info($"Record: {currentRecordName}");
                currentRecord[JsonVariables.RecordToken] = currentRecordName;

                currentRecordData = SharedMethods.SaveSectionData(wdbReader, false);
                for (int f = 0; f < wdbVars.FieldCount; f++)
                {
                    switch (wdbVars.StrtypelistValues[strtypelistIndex])
                    {
                        // bitpacked
                        case 0:
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

                                            Log.Info($"{wdbVars.Fields[f]}: {iTypedataVal}");
                                            currentRecord[wdbVars.Fields[f]] = iTypedataVal;

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

                                            Log.Info($"{wdbVars.Fields[f]}: {iTypedataVal}");
                                            currentRecord[wdbVars.Fields[f]] = iTypedataVal;

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

                                            Log.Info($"{wdbVars.Fields[f]}: {uTypeDataVal}");
                                            currentRecord[wdbVars.Fields[f]] = uTypeDataVal;

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

                                            Log.Info($"{wdbVars.Fields[f]}: {uTypeDataVal}");
                                            currentRecord[wdbVars.Fields[f]] = uTypeDataVal;

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

                                            Log.Info($"{wdbVars.Fields[f]}: {fTypeDataVal}");
                                            currentRecord[wdbVars.Fields[f]] = fTypeDataVal;

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

                                            Log.Info($"{wdbVars.Fields[f]}: {fTypeDataVal}");
                                            currentRecord[wdbVars.Fields[f]] = fTypeDataVal;

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

                                            Log.Info($"{strArrayTypeDictKey}: {strArrayTypeStringVal}");
                                            currentRecord[strArrayTypeDictKey] = strArrayTypeStringVal;

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
                        case 1:
                            var floatDataVal = SharedMethods.DeriveFloatFromSectionData(currentRecordData, currentRecordDataIndex, true);

                            Log.Info($"{wdbVars.Fields[f]}: {floatDataVal}");
                            currentRecord[wdbVars.Fields[f]] = floatDataVal;

                            strtypelistIndex++;
                            currentRecordDataIndex += 4;
                            break;

                        // !!string section offset
                        case 2:
                            var stringDataOffset = SharedMethods.DeriveUIntFromSectionData(currentRecordData, currentRecordDataIndex, true);
                            var derivedString = SharedMethods.DeriveStringFromArray(wdbVars.StringsData, (int)stringDataOffset);

                            Log.Info($"{wdbVars.Fields[f]}: {derivedString}");
                            currentRecord[wdbVars.Fields[f]] = derivedString;

                            strtypelistIndex++;
                            currentRecordDataIndex += 4;
                            break;

                        // uint value
                        case 3:
                            var uintDataVal = SharedMethods.DeriveUIntFromSectionData(currentRecordData, currentRecordDataIndex, true);

                            Log.Info($"{wdbVars.Fields[f]}: {uintDataVal}");
                            currentRecord[wdbVars.Fields[f]] = uintDataVal;

                            strtypelistIndex++;
                            currentRecordDataIndex += 4;
                            break;
                    }
                }

                records.Add(currentRecord);

                Log.Info("");

                strtypelistIndex = 0;
                currentRecordDataIndex = 0;
                sectionPos += 32;
            }

            return records;
        }
    }
}