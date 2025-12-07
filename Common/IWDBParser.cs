namespace WDBJsonTool.Common;

public interface IWDBParser
{
    WDBData Parse(string wdbFilePath);
}
