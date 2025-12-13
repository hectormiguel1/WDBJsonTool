using WDBJsonTool.Support;

namespace WDBJsonTool.XIII2LR.Extraction
{
    internal static class StrArrayParser
    {
        public static void SubSections(BinaryReader wdbReader, WDBVariablesXIII2LR wdbVars)
        {
            var readPos = wdbReader.BaseStream.Position;

            // !!strArray
            _ = wdbReader.BaseStream.Position = readPos;
            SharedMethods.CheckSectionName(wdbReader, wdbVars.StrArraySectionName);
            wdbVars.StrArrayData = SharedMethods.SaveSectionData(wdbReader, false);
            wdbVars.RecordCount--;

            // !!strArrayInfo
            _ = wdbReader.BaseStream.Position = readPos + 32;
            SharedMethods.CheckSectionName(wdbReader, wdbVars.StrArrayInfoSectionName);

            _ = wdbReader.BaseStream.Position = wdbReader.ReadBytesUInt32(true) + 2;
            wdbVars.OffsetsPerValue = wdbReader.ReadByte();
            wdbVars.BitsPerOffset = wdbReader.ReadByte();
            wdbVars.RecordCount--;

            Log.Finest("[StrArray]");
            Log.Finest($"Offsets per value: {wdbVars.OffsetsPerValue}");
            Log.Finest($"Bits per offset: {wdbVars.BitsPerOffset}");


            // !!strArrayList
            _ = wdbReader.BaseStream.Position = readPos + 64;
            SharedMethods.CheckSectionName(wdbReader, wdbVars.StrArrayListSectionName);
            wdbVars.StrArrayListData = SharedMethods.SaveSectionData(wdbReader, false);
            wdbVars.RecordCount--;
        }


        public static void ArrangeArrayData(WDBVariablesXIII2LR wdbVars)
        {
            // Collect all !!strArray Offsets

            for (var a = 0; a < wdbVars.StrArrayListData.Length; a += 4)
            {
                var tmpReadArray = new byte[4];
                Array.ConstrainedCopy(wdbVars.StrArrayListData, a, tmpReadArray, 0, 4);

                Array.Reverse(tmpReadArray);
                wdbVars.StrArrayOffsets.Add(BitConverter.ToUInt32(tmpReadArray, 0));
            }


            // Process numbered s# fields in
            // !structitem data

            foreach (var fieldItem in wdbVars.Fields)
            {
                if (fieldItem.StartsWith("s"))
                {
                    var fieldNumber = SharedMethods.DeriveFieldNumber(fieldItem);

                    if (fieldNumber != 0)
                    {
                        wdbVars.NumStringFields.Add(fieldItem);
                    }
                }
            }

            if (wdbVars.StrArrayOffsets.Count != wdbVars.NumStringFields.Count)
            {
                SharedMethods.ErrorExit("StrArrayOffsets count does not match with the detected amount of s# fields");
            }


            // Copy !!strArray data into a stream and 
            // open a reader for this stream
            using (var strArrayStream = new MemoryStream())
            {
                using (var strArrayReader = new BinaryReader(strArrayStream))
                {
                    var strArrayDataLen = wdbVars.StrArrayData.Length;
                    strArrayStream.Write(wdbVars.StrArrayData, 0, strArrayDataLen);
                    strArrayStream.Seek(0, SeekOrigin.Begin);

                    // Copy !!strings data into a stream and 
                    // open a reader for this stream
                    using (var stringsStream = new MemoryStream())
                    {
                        using (var stringsReader = new BinaryReader(stringsStream))
                        {
                            stringsStream.Write(wdbVars.StringsData, 0, wdbVars.StringsData.Length);
                            stringsStream.Seek(0, SeekOrigin.Begin);


                            var arrayIterator = 0;
                            uint currentArrayItemIndex = 0;
                            var buildArray = true;

                            foreach (var listOffset in wdbVars.StrArrayOffsets)
                            {
                                wdbVars.ProcessStringsList = [];

                                while (buildArray)
                                {
                                    // Get the value with which the string
                                    // offset values will be derived and
                                    // convert the value to binary
                                    //
                                    // 'binaryReadPos' value will be read from
                                    // right to left
                                    var strArrayBinaryUInt = strArrayReader.ReadBytesUInt32(true);
                                    var strArrayBinary = strArrayBinaryUInt.UIntToBinary();
                                    var binaryReadPos = strArrayBinary.Length - wdbVars.BitsPerOffset;

                                    // Get each offset's values
                                    for (var o = 0; o < wdbVars.OffsetsPerValue; o++)
                                    {
                                        var stringOffset = BitOperationHelpers.BinaryToUInt(strArrayBinary, binaryReadPos, wdbVars.BitsPerOffset);

                                        stringsReader.BaseStream.Position = stringOffset;
                                        var currentString = stringsReader.ReadStringTillNull();

                                        //if (currentString == "")
                                        //{
                                        //    currentString = "{null}";
                                        //}

                                        wdbVars.ProcessStringsList.Add(currentString);

                                        currentArrayItemIndex++;
                                        binaryReadPos -= wdbVars.BitsPerOffset;
                                    }

                                    if (strArrayReader.BaseStream.Position == strArrayDataLen)
                                    {
                                        buildArray = false;
                                    }

                                    if (wdbVars.StrArrayOffsets.Contains((uint)strArrayReader.BaseStream.Position))
                                    {
                                        buildArray = false;
                                    }
                                }

                                currentArrayItemIndex = 0;

                                wdbVars.StrArrayDict.Add(wdbVars.NumStringFields[arrayIterator], wdbVars.ProcessStringsList);
                                //Console.WriteLine($"Built [{wdbVars.NumStringFields[arrayIterator]}]");

                                if (arrayIterator + 1 == wdbVars.StrArrayOffsets.Count) continue;
                                arrayIterator = wdbVars.StrArrayOffsets.IndexOf((uint)strArrayReader.BaseStream.Position);

                                buildArray = true;
                            }
                        }
                    }
                }
            }

            Log.Fine($"Finished organizing {wdbVars.StrArraySectionName}");
        }
    }
}