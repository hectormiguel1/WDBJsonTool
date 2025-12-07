using System.Text;
using WDBJsonTool.Support;
using WDBJsonTool.Extensions;
using WDBJsonTool.Common;
using WDBJsonTool.Common.FieldProcessors;

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
                    RecordConversionHelper.WriteUInt32Value(wdbVars.StrArrayListData, listStartOffset, arrayStartOffset);

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
                    case (int)WdbFieldType.Bitpacked:
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
                                    Log.Debug($"{wdbVars.Fields[f]}: {iTypeDataVal}");

                                    var iResult = RecordConversionHelper.PackIntField(iTypeDataVal, fieldNum, ref fieldBitsToProcess, ref collectedBinary);
                                    if (iResult == "overflow")
                                    {
                                        f--;
                                        fieldBitsToProcess = 0;
                                        continue;
                                    }

                                    if (fieldBitsToProcess != 0)
                                    {
                                        f++;
                                    }
                                    break;

                                // uint
                                case "u":
                                    uTypeDataVal = Convert.ToUInt32(recordData.Value[f]);
                                    Log.Debug($"{wdbVars.Fields[f]}: {uTypeDataVal}");

                                    var uResult = RecordConversionHelper.PackUIntField(uTypeDataVal, fieldNum, ref fieldBitsToProcess, ref collectedBinary);
                                    if (uResult == "overflow")
                                    {
                                        f--;
                                        fieldBitsToProcess = 0;
                                        continue;
                                    }

                                    if (fieldBitsToProcess != 0)
                                    {
                                        f++;
                                    }
                                    break;

                                // float (bitpacked as int)
                                case "f":
                                    fTypeDataVal = Convert.ToInt32(recordData.Value[f]);
                                    Log.Debug($"{wdbVars.Fields[f]}: {fTypeDataVal}");

                                    var fResult = RecordConversionHelper.PackFloatField(fTypeDataVal, fieldNum, ref fieldBitsToProcess, ref collectedBinary);
                                    if (fResult == "overflow")
                                    {
                                        f--;
                                        fieldBitsToProcess = 0;
                                        continue;
                                    }

                                    if (fieldBitsToProcess != 0)
                                    {
                                        f++;
                                    }
                                    break;

                                // (s#) strArray item index
                                case "s":
                                    var stringItem = recordData.Value[f].ToString();
                                    sTypeDataVal = (uint)_strArrayDataDict[wdbVars.Fields[f]].IndexOf(stringItem);
                                    Log.Debug($"{wdbVars.Fields[f]}: {stringItem} | Index: {sTypeDataVal}");

                                    var sResult = RecordConversionHelper.PackStrArrayField(sTypeDataVal, fieldNum, ref fieldBitsToProcess, ref collectedBinary);
                                    if (sResult == "overflow")
                                    {
                                        f--;
                                        fieldBitsToProcess = 0;
                                        continue;
                                    }

                                    if (fieldBitsToProcess != 0)
                                    {
                                        f++;
                                    }
                                    break;
                            }
                        }

                        RecordConversionHelper.WriteBitpackedValue(currentOutData, dataIndex, collectedBinary);

                        strtypelistIndex++;
                        dataIndex += 4;
                        break;

                    // float value
                    case (int)WdbFieldType.Float:
                        var floatVal = Convert.ToSingle(recordData.Value[f]);
                        Log.Debug($"{wdbVars.Fields[f]}: {floatVal}");

                        RecordConversionHelper.WriteFloatValue(currentOutData, dataIndex, floatVal);

                        strtypelistIndex++;
                        dataIndex += 4;
                        break;

                    // string section offset
                    case (int)WdbFieldType.String:
                        var stringVal = recordData.Value[f].ToString();
                        Log.Debug($"{wdbVars.Fields[f]}: {stringVal}");

                        stringPos = RecordConversionHelper.ProcessStringField(
                            currentOutData,
                            dataIndex,
                            stringVal,
                            wdbVars.ProcessedStringsDict,
                            stringPos);

                        strtypelistIndex++;
                        dataIndex += 4;
                        break;

                    // uint value
                    case (int)WdbFieldType.UInt:
                        var uintVal = Convert.ToUInt32(recordData.Value[f]);
                        Log.Debug($"{wdbVars.Fields[f]}: {uintVal}");

                        RecordConversionHelper.WriteUInt32Value(currentOutData, dataIndex, uintVal);

                        strtypelistIndex++;
                        dataIndex += 4;
                        break;
                }
            }

                            wdbVars.OutPerRecordData.Add(recordData.Key, currentOutData);
        }
    }
}
