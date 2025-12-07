using System.Buffers.Binary;

namespace WDBJsonTool.Common.FieldProcessors;

/// <summary>
/// Processes string offset fields (32-bit offset into !!string section).
/// </summary>
public class StringOffsetFieldProcessor : IFieldProcessor<uint>
{
    public int SizeInBytes => 4;

    public uint Extract(ReadOnlySpan<byte> data, int offset)
    {
        return BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
    }

    public void Write(Span<byte> data, int offset, uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(data.Slice(offset, 4), value);
    }
}
