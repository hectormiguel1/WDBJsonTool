using System.Text;
using WDBJsonTool.Common;
using WDBJsonTool.Common.FieldProcessors;
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
                    case (int)WdbFieldType.Bitpacked:
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
                        if (wdbVars.Fields[f].StartsWith("u64"))
                        {
                            var ulongVal = Convert.ToUInt64(recordData.Value[f]);
                            Log.Debug($"{wdbVars.Fields[f]}: {ulongVal}");

                            RecordConversionHelper.WriteUInt64Value(currentOutData, dataIndex, ulongVal);

                            strtypelistIndex += 2;
                            dataIndex += 8;
                        }
                        else
                        {
                            var uintVal = Convert.ToUInt32(recordData.Value[f]);
                            Log.Debug($"{wdbVars.Fields[f]}: {uintVal}");

                            RecordConversionHelper.WriteUInt32Value(currentOutData, dataIndex, uintVal);

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
                    case (int)WdbFieldType.Bitpacked:
                        var bitpackedBinary = (string)recordData.Value[f];
                        Log.Debug($"bitpacked-field_{bitpackedFieldCounter}: {bitpackedBinary}");

                        var bitpackedValue = Convert.ToUInt32(bitpackedBinary.Substring(2), 16);
                        RecordConversionHelper.WriteUInt32Value(currentOutData, dataIndex, bitpackedValue);

                        strtypelistIndex++;
                        dataIndex += 4;
                        bitpackedFieldCounter++;
                        break;

                    // float value
                    case (int)WdbFieldType.Float:
                        var floatVal = Convert.ToSingle(recordData.Value[f]);
                        Log.Debug($"float-field_{floatFieldCounter}: {floatVal}");

                        RecordConversionHelper.WriteFloatValue(currentOutData, dataIndex, floatVal);

                        strtypelistIndex++;
                        dataIndex += 4;
                        floatFieldCounter++;
                        break;

                    // string section offset
                    case (int)WdbFieldType.String:
                        var stringVal = recordData.Value[f].ToString();
                        Log.Debug($"!!string-field_{stringFieldCounter}: {stringVal}");

                        stringPos = RecordConversionHelper.ProcessStringField(
                            currentOutData,
                            dataIndex,
                            stringVal,
                            wdbVars.ProcessedStringsDict,
                            stringPos);

                        strtypelistIndex++;
                        dataIndex += 4;
                        stringFieldCounter++;
                        break;

                    // uint value
                    case (int)WdbFieldType.UInt:
                        var uintVal = Convert.ToUInt32(recordData.Value[f]);
                        Log.Debug($"uint-field_{uintFieldCounter}: {uintVal}");

                        RecordConversionHelper.WriteUInt32Value(currentOutData, dataIndex, uintVal);

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
