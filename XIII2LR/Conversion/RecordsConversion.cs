using System.Text;
using WDBJsonTool.Support;
using WDBJsonTool.Extensions;

namespace WDBJsonTool.XIII2LR.Conversion;
internal class RecordsConversion
{
    private static Dictionary<string, List<string>> _strArrayDataDict = new();

    public static void ConvertRecordsStrArray(WDBVariablesXIII2LR wdbVars)
    {
        Log.Info("Building strArray....");

        foreach (var recordData in wdbVars.RecordsDataDict)
        {
            for (int f = 0; f < wdbVars.FieldCount; f++)
            {
                var fieldType = wdbVars.Fields[f].Substring(0, 1);
                var fieldNum = SharedMethods.DeriveFieldNumber(wdbVars.Fields[f]);

                if (fieldType == "s" && fieldNum != 0)
                {
                    var currentSField = wdbVars.Fields[f];
                    var currentString = recordData.Value[f].ToString();

                    if (!_strArrayDataDict.ContainsKey(currentSField))
                    {
                        _strArrayDataDict.Add(currentSField, new List<string>());
                    }

                    if (!_strArrayDataDict[currentSField].Contains(currentString))
                    {
                        _strArrayDataDict[currentSField].Add(currentString);
                    }
                }
            }
        }

        uint stringPos = 1;
        wdbVars.ProcessedStringsDict.Add("", 0);
        var strArrayValDict = new Dictionary<string, List<uint>>();

        foreach (var strArrayData in _strArrayDataDict)
        {
            var currentArrayName = strArrayData.Key;
            var currentArrayList = strArrayData.Value;
            var lastItemNumber = currentArrayList.Count;
            var addedString = false;

            strArrayValDict.Add(currentArrayName, new List<uint>());

            for (int s = 0; s < currentArrayList.Count; s++)
            {
                var currentValBinaryList = new List<string>();

                for (int o = 0; o < wdbVars.OffsetsPerValue; o++)
                {
                    var currentStringItem = currentArrayList[s];

                    if (!wdbVars.ProcessedStringsDict.ContainsKey(currentStringItem))
                    {
                        wdbVars.ProcessedStringsDict.Add(currentStringItem, stringPos);
                        addedString = true;
                    }

                    var stringItemPos = wdbVars.ProcessedStringsDict[currentStringItem];
                    var currentOffsetVal = stringItemPos.UIntToBinaryFixed(wdbVars.BitsPerOffset);
                    currentValBinaryList.Add(currentOffsetVal);

                    if (addedString)
                    {
                        stringPos += (uint)Encoding.UTF8.GetByteCount(currentStringItem + "\0");
                        addedString = false;
                    }

                    s++;

                    if (s == lastItemNumber)
                    {
                        break;
                    }
                }

                s--;
                currentValBinaryList.Reverse();
                var currentValBinary = string.Join("", currentValBinaryList);

                strArrayValDict[currentArrayName].Add(Convert.ToUInt32(currentValBinary, 2));
            }
        }


        wdbVars.StrArrayListData = new byte[strArrayValDict.Count * 4];
        var listStartOffset = 0;
        uint arrayStartOffset = 0;

        using (var strArrayStream = new MemoryStream())
        {
            using (var strArrayWriter = new BinaryWriter(strArrayStream))
            {
                foreach (var strArrayValue in strArrayValDict)
                {
                    var arrayStartOffsetBytes = BitConverter.GetBytes(arrayStartOffset);
                    wdbVars.StrArrayListData[listStartOffset] = arrayStartOffsetBytes[3];
                    wdbVars.StrArrayListData[listStartOffset + 1] = arrayStartOffsetBytes[2];
                    wdbVars.StrArrayListData[listStartOffset + 2] = arrayStartOffsetBytes[1];
                    wdbVars.StrArrayListData[listStartOffset + 3] = arrayStartOffsetBytes[0];

                    var valueCount = (uint)strArrayValue.Value.Count;

                    foreach (var value in strArrayValue.Value)
                    {
                        strArrayWriter.WriteBytesUInt32(value, true);
                    }

                    arrayStartOffset += valueCount * 4;
                    listStartOffset += 4;
                }

                strArrayStream.Seek(0, SeekOrigin.Begin);
                wdbVars.StrArrayData = new byte[] { };
                wdbVars.StrArrayData = strArrayStream.ToArray();
            }
        }

        strArrayValDict.Clear();

        WriteFieldValuesForRecords(wdbVars, stringPos);

        _strArrayDataDict.Clear();
    }


    public static void ConvertRecords(WDBVariablesXIII2LR wdbVars)
    {
        uint stringPos = 1;
        wdbVars.ProcessedStringsDict.Add("", 0);

        WriteFieldValuesForRecords(wdbVars, stringPos);
    }


    private static void WriteFieldValuesForRecords(WDBVariablesXIII2LR wdbVars, uint stringPos)
    {
        Log.Info("Building records....");

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
                        uint sTypeDataVal;

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

                                // (s#) strArray item index
                                case "s":
                                    var stringItem = recordData.Value[f].ToString();
                                    sTypeDataVal = (uint)_strArrayDataDict[wdbVars.Fields[f]].IndexOf(stringItem);

                                    if (fieldNum != 0)
                                    {
                                        SharedMethods.ValidateUInt(fieldNum, ref sTypeDataVal);
                                    }

                                    Log.Debug($"{wdbVars.Fields[f]}: {stringItem} | Index: {sTypeDataVal}");

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
                                        var sTypedataValBinary = sTypeDataVal.UIntToBinaryFixed(fieldNum).ReverseBinary();
                                        collectedBinary += sTypedataValBinary;

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
                        var uintVal = Convert.ToUInt32(recordData.Value[f]);
                        Log.Debug($"{wdbVars.Fields[f]}: {uintVal}");

                        var uintValBytes = BitConverter.GetBytes(uintVal);

                        currentOutData[dataIndex] = uintValBytes[3];
                        currentOutData[dataIndex + 1] = uintValBytes[2];
                        currentOutData[dataIndex + 2] = uintValBytes[1];
                        currentOutData[dataIndex + 3] = uintValBytes[0];

                        strtypelistIndex++;
                        dataIndex += 4;
                        break;
                }
            }

                            wdbVars.OutPerRecordData.Add(recordData.Key, currentOutData);
        }
    }
}
