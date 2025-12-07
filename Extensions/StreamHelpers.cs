namespace WDBJsonTool.Extensions;

public static class StreamHelpers
{
    public static void CopyStreamTo(this Stream inStream, Stream outStream, long size, bool showProgress)
    {
        const int bufferSize = 81920;
        var amountRemaining = size;
        long amountCopied = 0;

        while (amountRemaining > 0)
        {
            var arraySize = Math.Min(bufferSize, amountRemaining);
            var copyArray = new byte[arraySize];

            _ = inStream.Read(copyArray, 0, (int)arraySize);
            outStream.Write(copyArray, 0, (int)arraySize);

            amountRemaining -= arraySize;

            amountCopied += arraySize;

            if (!showProgress) continue;
            var currentAmount = Math.Round(((decimal)amountCopied / size) * 100);
            Log.Debug($"Copied {currentAmount}%");
        }
    }


    public static void PadNull(this Stream stream, int padAmount)
    {
        for (var p = 0; p < padAmount; p++)
        {
            stream.WriteByte(0);
        }
    }
}