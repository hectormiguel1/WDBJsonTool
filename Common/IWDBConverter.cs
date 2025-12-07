namespace WDBJsonTool.Common;

public interface IWDBConverter
{
    void Convert(WDBData data, string jsonFilePath);
}
