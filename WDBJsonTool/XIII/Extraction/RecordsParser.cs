using System.Text.Json;
using WDBJsonTool.Support;
using WDBJsonTool.DataStructures;
using WDBJsonTool;

namespace WDBJsonTool.XIII.Extraction
{
    internal class RecordsParser
    {
        public static List<WDBRecord> ParseRecordsWithFields(BinaryReader wdbReader, WDBVariablesXIII wdbVars)
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
                            if (wdbVars.Fields[f].StartsWith("u64"))
                            {
                                var processArray = new byte[8];
                                Array.ConstrainedCopy(currentRecordData, currentRecordDataIndex, processArray, 0, 8);
                                Array.Reverse(processArray);

                                var ulTypeDataVal = BitConverter.ToUInt64(processArray, 0);

                                Log.Info($"{wdbVars.Fields[f]}(uint64): {ulTypeDataVal}");
                                currentRecord[$"{wdbVars.Fields[f]}(uint64)"] = ulTypeDataVal;

                                strtypelistIndex++;
                                currentRecordDataIndex += 8;
                                break;
                            }

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


        public static List<WDBRecord> ParseRecordsWithoutFields(BinaryReader wdbReader, WDBVariablesXIII wdbVars)
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

                var bitpackedFieldCounter = 0;
                var floatFieldCounter = 0;
                var stringFieldCounter = 0;
                var uintFieldCounter = 0;

                for (int f = 0; f < wdbVars.FieldCount; f++)
                {
                    switch (wdbVars.StrtypelistValues[strtypelistIndex])
                    {
                        // bitpacked
                        case 0:
                            var bitpackedData = SharedMethods.DeriveUIntFromSectionData(currentRecordData, currentRecordDataIndex, true);
                            var hexDataVal = "0x" + bitpackedData.ToString("X").PadLeft(8, '0');

                            Log.Info($"bitpacked-field_{bitpackedFieldCounter}: {hexDataVal}");
                            currentRecord[$"bitpacked-field_{bitpackedFieldCounter}"] = hexDataVal;

                            strtypelistIndex++;
                            currentRecordDataIndex += 4;
                            bitpackedFieldCounter++;
                            break;

                        // float value
                        case 1:
                            var floatDataVal = SharedMethods.DeriveFloatFromSectionData(currentRecordData, currentRecordDataIndex, true);

                            Log.Info($"float-field_{floatFieldCounter}: {floatDataVal}");
                            currentRecord[$"float-field_{floatFieldCounter}"] = floatDataVal;

                            strtypelistIndex++;
                            currentRecordDataIndex += 4;
                            floatFieldCounter++;
                            break;

                        // !!string section offset
                        case 2:
                            var stringDataOffset = SharedMethods.DeriveUIntFromSectionData(currentRecordData, currentRecordDataIndex, true);
                            var derivedString = SharedMethods.DeriveStringFromArray(wdbVars.StringsData, (int)stringDataOffset);

                            Log.Info($"!!string-field_{stringFieldCounter}: {derivedString}");
                            currentRecord[$"!!string-field_{stringFieldCounter}"] = derivedString;

                            strtypelistIndex++;
                            currentRecordDataIndex += 4;
                            stringFieldCounter++;
                            break;

                        // uint value
                        case 3:
                            var uintDataVal = SharedMethods.DeriveUIntFromSectionData(currentRecordData, currentRecordDataIndex, true);

                            Log.Info($"uint-field_{uintFieldCounter}: {uintDataVal}");
                            currentRecord[$"uint-field_{uintFieldCounter}"] = uintDataVal;

                            strtypelistIndex++;
                            currentRecordDataIndex += 4;
                            uintFieldCounter++;
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