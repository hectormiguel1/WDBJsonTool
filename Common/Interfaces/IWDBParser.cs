using WDBJsonTool.Common;

namespace WDBJsonTool.Common.Interfaces;
public interface IWDBParser
{
    WDBData Parse(byte[] data);
}
