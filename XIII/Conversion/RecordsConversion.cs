using System.Text;
using WDBJsonTool.Extensions;
using WDBJsonTool.Support;

namespace WDBJsonTool.XIII.Conversion;
internal class RecordsConversion
{
    public static void ConvertRecordsWithFields(WDBVariablesXIII wdbVars)
    {
        uint stringPos = 1;
        wdbVars.ProcessedStringsDict.Add("", 0);

        var outPerRecordSize = wdbVars.StrtypelistValues.Count * 4;
        foreach (var recordData in wdbVars.RecordsDataDict)
        {
            var currentOutData = new byte[outPerRecordSize];

            Log.Debug($"Record: {recordData.Key}");

            var dataIndex = 0;
            var strtypelistIndex = 0;

            for (int f = 0; f < wdbVars.FieldCount; f++)
            {
                var fieldBitsToProcess = 32;
                var collectedBinary = string.Empty;
                var addedString = false;

                switch (wdbVars.StrtypelistValues[strtypelistIndex])
                {
                    // bitpacked
                    case 0:
                        int iTypeDataVal;
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
                                    iTypeDataVal = Convert.ToInt32(recordData.Value[f]);

                                    if (fieldNum != 0)
                                    {
                                        SharedMethods.ValidateInt(fieldNum, ref iTypeDataVal);
                                    }

                                    Log.Debug($"{wdbVars.Fields[f]}: {iTypeDataVal}");

                                    if (fieldNum == 0)
                                    {
                                        fieldNum = 32;
                                    }

                                    if (fieldNum > fieldBitsToProcess)
                                    {
                                        f--;
                                        fieldBitsToProcess = 0;
                                        continue;
                                    }
                                    else
                                    {
                                        var iTypedataValBinary = iTypeDataVal.IntToBinaryFixed(fieldNum);

                                        if (iTypedataValBinary.Length > fieldNum)
                                        {
                                            iTypedataValBinary = iTypedataValBinary.Substring(iTypedataValBinary.Length - fieldNum, fieldNum);
                                        }

                                        iTypedataValBinary = iTypedataValBinary.ReverseBinary();
                                        collectedBinary += iTypedataValBinary;

                                        fieldBitsToProcess -= fieldNum;

                                        if (fieldBitsToProcess != 0)
                                        {
                                            f++;
                                        }
                                    }
                                    break;

                                // uint 
                                case "u":
                                    uTypeDataVal = Convert.ToUInt32(recordData.Value[f]);

                                    if (fieldNum != 0)
                                    {
                                        SharedMethods.ValidateUInt(fieldNum, ref uTypeDataVal);
                                    }

                                    Log.Debug($"{wdbVars.Fields[f]}: {uTypeDataVal}");

                                    if (fieldNum == 0)
                                    {
                                        fieldNum = 32;
                                    }

                                    if (fieldNum > fieldBitsToProcess)
                                    {
                                        f--;
                                        fieldBitsToProcess = 0;
                                        continue;
                                    }
                                    else
                                    {
                                        var uTypedataValBinary = uTypeDataVal.UIntToBinaryFixed(fieldNum).ReverseBinary();
                                        collectedBinary += uTypedataValBinary;

                                        fieldBitsToProcess -= fieldNum;

                                        if (fieldBitsToProcess != 0)
                                        {
                                            f++;
                                        }
                                    }
                                    break;

                                // float (bitpacked as int)
                                case "f":
                                    fTypeDataVal = Convert.ToInt32(recordData.Value[f]);

                                    if (fieldNum != 0)
                                    {
                                        SharedMethods.ValidateInt(fieldNum, ref fTypeDataVal);
                                    }

                                    Log.Debug($"{wdbVars.Fields[f]}: {fTypeDataVal}");

                                    if (fieldNum == 0)
                                    {
                                        fieldNum = 32;
                                    }

                                    if (fieldNum > fieldBitsToProcess)
                                    {
                                        f--;
                                        fieldBitsToProcess = 0;
                                        continue;
                                    }
                                    else
                                    {
                                        var fTypedataValBinary = fTypeDataVal.IntToBinaryFixed(fieldNum);

                                        if (fTypedataValBinary.Length > fieldNum)
                                        {
                                            fTypedataValBinary = fTypedataValBinary.Substring(fTypedataValBinary.Length - fieldNum, fieldNum);
                                        }

                                        fTypedataValBinary = fTypedataValBinary.ReverseBinary();
                                        collectedBinary += fTypedataValBinary;

                                        fieldBitsToProcess -= fieldNum;

                                        if (fieldBitsToProcess != 0)
                                        {
                                            f++;
                                        }
                                    }
                                    break;
                            }
                        }

                        collectedBinary = collectedBinary.ReverseBinary();
                        var collectiveBinaryBytes = BitConverter.GetBytes(Convert.ToUInt32(collectedBinary, 2));

                        currentOutData[dataIndex] = collectiveBinaryBytes[3];
                        currentOutData[dataIndex + 1] = collectiveBinaryBytes[2];
                        currentOutData[dataIndex + 2] = collectiveBinaryBytes[1];
                        currentOutData[dataIndex + 3] = collectiveBinaryBytes[0];

                        strtypelistIndex++;
                        dataIndex += 4;
                        break;

                    // float value
                    case 1:
                        var floatVal = Convert.ToSingle(recordData.Value[f]);
                        Log.Debug($"{wdbVars.Fields[f]}: {floatVal}");

                        var floatValBytes = BitConverter.GetBytes(floatVal);

                        currentOutData[dataIndex] = floatValBytes[3];
                        currentOutData[dataIndex + 1] = floatValBytes[2];
                        currentOutData[dataIndex + 2] = floatValBytes[1];
                        currentOutData[dataIndex + 3] = floatValBytes[0];

                        strtypelistIndex++;
                        dataIndex += 4;
                        break;

                    // string section offset
                    case 2:
                        var stringVal = recordData.Value[f].ToString();
                        Log.Debug($"{wdbVars.Fields[f]}: {stringVal}");

                        if (stringVal != "")
                        {
                            if (!wdbVars.ProcessedStringsDict.ContainsKey(stringVal))
                            {
                                wdbVars.ProcessedStringsDict.Add(stringVal, stringPos);
                                addedString = true;
                            }

                            var stringPosBytes = BitConverter.GetBytes(wdbVars.ProcessedStringsDict[stringVal]);

                            currentOutData[dataIndex] = stringPosBytes[3];
                            currentOutData[dataIndex + 1] = stringPosBytes[2];
                            currentOutData[dataIndex + 2] = stringPosBytes[1];
                            currentOutData[dataIndex + 3] = stringPosBytes[0];

                            if (addedString)
                            {
                                stringPos += (uint)Encoding.UTF8.GetByteCount(stringVal + "\0");
                                addedString = false;
                            }
                        }

                        strtypelistIndex++;
                        dataIndex += 4;
                        break;

                    // uint value
                    case 3:
                        if (wdbVars.Fields[f].StartsWith("u64"))
                        {
                            var ulongVal = Convert.ToUInt64(recordData.Value[f]);
                            Log.Debug($"{wdbVars.Fields[f]}: {ulongVal}");

                            var ulongValBytes = BitConverter.GetBytes(ulongVal);

                            currentOutData[dataIndex] = ulongValBytes[7];
                            currentOutData[dataIndex + 1] = ulongValBytes[6];
                            currentOutData[dataIndex + 2] = ulongValBytes[5];
                            currentOutData[dataIndex + 3] = ulongValBytes[4];
                            currentOutData[dataIndex + 4] = ulongValBytes[3];
                            currentOutData[dataIndex + 5] = ulongValBytes[2];
                            currentOutData[dataIndex + 6] = ulongValBytes[1];
                            currentOutData[dataIndex + 7] = ulongValBytes[0];

                            strtypelistIndex += 2;
                            dataIndex += 8;
                        }
                        else
                        {
                            var uintVal = Convert.ToUInt32(recordData.Value[f]);
                            Log.Debug($"{wdbVars.Fields[f]}: {uintVal}");

                            var uintValBytes = BitConverter.GetBytes(uintVal);

                            currentOutData[dataIndex] = uintValBytes[3];
                            currentOutData[dataIndex + 1] = uintValBytes[2];
                            currentOutData[dataIndex + 2] = uintValBytes[1];
                            currentOutData[dataIndex + 3] = uintValBytes[0];

                            strtypelistIndex++;
                            dataIndex += 4;
                        }
                        break;
                }
            }

            
            wdbVars.OutPerRecordData.Add(recordData.Key, currentOutData);
        }
    }


    public static void ConvertRecordsNoFields(WDBVariablesXIII wdbVars)
    {
        uint stringPos = 1;
        wdbVars.ProcessedStringsDict.Add("", 0);

        var outPerRecordSize = wdbVars.StrtypelistValues.Count * 4;
        foreach (var recordData in wdbVars.RecordsDataDict)
        {
            var currentOutData = new byte[outPerRecordSize];

            Log.Debug($"Record: {recordData.Key}");

            var dataIndex = 0;
            var strtypelistIndex = 0;
            var bitpackedFieldCounter = 0;
            var floatFieldCounter = 0;
            var stringFieldCounter = 0;
            var uintFieldCounter = 0;

            for (int f = 0; f < wdbVars.FieldCount; f++)
            {
                var collectedBinary = string.Empty;
                var addedString = false;

                switch (wdbVars.StrtypelistValues[strtypelistIndex])
                {
                    // bitpacked
                    case 0:
                        var bitpackedBinary = (string)recordData.Value[f];
                        Log.Debug($"bitpacked-field_{bitpackedFieldCounter}: {bitpackedBinary}");

                        var bitpackedBinaryBytes = BitConverter.GetBytes(Convert.ToUInt32(bitpackedBinary.Substring(2), 16));

                        currentOutData[dataIndex] = bitpackedBinaryBytes[3];
                        currentOutData[dataIndex + 1] = bitpackedBinaryBytes[2];
                        currentOutData[dataIndex + 2] = bitpackedBinaryBytes[1];
                        currentOutData[dataIndex + 3] = bitpackedBinaryBytes[0];

                        strtypelistIndex++;
                        dataIndex += 4;
                        bitpackedFieldCounter++;
                        break;


                    // float value
                    case 1:
                        var floatVal = Convert.ToSingle(recordData.Value[f]);
                        Log.Debug($"float-field_{floatFieldCounter}: {floatVal}");

                        var floatValBytes = BitConverter.GetBytes(floatVal);

                        currentOutData[dataIndex] = floatValBytes[3];
                        currentOutData[dataIndex + 1] = floatValBytes[2];
                        currentOutData[dataIndex + 2] = floatValBytes[1];
                        currentOutData[dataIndex + 3] = floatValBytes[0];

                        strtypelistIndex++;
                        dataIndex += 4;
                        floatFieldCounter++;
                        break;


                    // string section offset
                    case 2:
                        var stringVal = recordData.Value[f].ToString();
                        Log.Debug($"!!string-field_{stringFieldCounter}: {stringVal}");

                        if (stringVal != "")
                        {
                            if (!wdbVars.ProcessedStringsDict.ContainsKey(stringVal))
                            {
                                wdbVars.ProcessedStringsDict.Add(stringVal, stringPos);
                                addedString = true;
                            }

                            var stringPosBytes = BitConverter.GetBytes(wdbVars.ProcessedStringsDict[stringVal]);

                            currentOutData[dataIndex] = stringPosBytes[3];
                            currentOutData[dataIndex + 1] = stringPosBytes[2];
                            currentOutData[dataIndex + 2] = stringPosBytes[1];
                            currentOutData[dataIndex + 3] = stringPosBytes[0];

                            if (addedString)
                            {
                                stringPos += (uint)Encoding.UTF8.GetByteCount(stringVal + "\0");
                                addedString = false;
                            }
                        }

                        strtypelistIndex++;
                        dataIndex += 4;
                        stringFieldCounter++;
                        break;


                    // uint value
                    case 3:
                        var uintVal = Convert.ToUInt32(recordData.Value[f]);
                        Log.Debug($"uint-field_{uintFieldCounter}: {uintVal}");

                        var uintValBytes = BitConverter.GetBytes(uintVal);

                        currentOutData[dataIndex] = uintValBytes[3];
                        currentOutData[dataIndex + 1] = uintValBytes[2];
                        currentOutData[dataIndex + 2] = uintValBytes[1];
                        currentOutData[dataIndex + 3] = uintValBytes[0];

                        strtypelistIndex++;
                        dataIndex += 4;
                        uintFieldCounter++;
                        break;
                }
            }

            
            wdbVars.OutPerRecordData.Add(recordData.Key, currentOutData);
        }
    }
}
