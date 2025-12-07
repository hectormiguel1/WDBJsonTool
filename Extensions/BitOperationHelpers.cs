public static class BitOperationHelpers
{
    public static string UShortToBinary(this ushort valToParse)
    {
        var valArray = BitConverter.GetBytes(valToParse);

        return Convert.ToString(valArray[1], 2).PadLeft(8, '0') + "" +
            Convert.ToString(valArray[0], 2).PadLeft(8, '0');
    }


    public static string ReverseBinary(this string binaryVal)
    {
        var reversedBinaryVal = string.Empty;

        for (int i = binaryVal.Length; i > 0; i--)
        {
            reversedBinaryVal += binaryVal[i - 1];
        }

        return reversedBinaryVal;
    }


    public static string UIntToBinary(this uint valToParse)
    {
        var valArray = BitConverter.GetBytes(valToParse);

        return Convert.ToString(valArray[3], 2).PadLeft(8, '0') + "" +
            Convert.ToString(valArray[2], 2).PadLeft(8, '0') + "" +
            Convert.ToString(valArray[1], 2).PadLeft(8, '0') + "" +
            Convert.ToString(valArray[0], 2).PadLeft(8, '0');
    }


    public static string UIntToBinaryFixed(this uint valToParse, int fixAmount)
    {
        return Convert.ToString(valToParse, 2).PadLeft(fixAmount, '0');
    }


    public static string IntToBinaryFixed(this int valToParse, int fixAmount)
    {
        if (valToParse < 0)
        {
            var computedVal = Convert.ToString(valToParse, 2);
            return computedVal.Substring(computedVal.Length - fixAmount);
        }
        else
        {
            return Convert.ToString(valToParse, 2).PadLeft(fixAmount, '0');
        }
    }


    public static uint BinaryToUInt(this string binaryVal, int startPosition, int count)
    {
        return Convert.ToUInt32(binaryVal.Substring(startPosition, count), 2);
    }


    public static int BinaryToInt(this string binaryVal, int startPosition, int count)
    {
        var pass1 = binaryVal.Substring(startPosition, count);

        if (pass1[0] == '0')
        {
            return (int)Convert.ToUInt32(pass1, 2);
        }
        else
        {
            int pass1BinaryVal;
            var iterationCount = count - 1;
            int computedValWithPower;

            var significantBitPower = -(int)Math.Pow(2, iterationCount);
            var finalComputedVal = significantBitPower;

            for (int i = 0; i < iterationCount; i++)
            {
                pass1BinaryVal = int.Parse(pass1[i + 1].ToString());
                computedValWithPower = (int)Math.Pow(2, (count - 2) - i);
                finalComputedVal += (computedValWithPower * pass1BinaryVal);
            }

            return finalComputedVal;
        }
    }
}