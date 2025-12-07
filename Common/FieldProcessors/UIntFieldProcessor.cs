using System.Buffers.Binary;

namespace WDBJsonTool.Common.FieldProcessors;

/// <summary>
/// Processes 32-bit or 64-bit unsigned integer fields.
/// </summary>
public class UIntFieldProcessor(bool is64Bit = false) : IFieldProcessor<ulong>
{
    public int SizeInBytes => is64Bit ? 8 : 4;

    public ulong Extract(ReadOnlySpan<byte> data, int offset)
    {
        return is64Bit ? BinaryPrimitives.ReadUInt64BigEndian(data.Slice(offset, 8)) : BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
    }

    public void Write(Span<byte> data, int offset, ulong value)
    {
        if (is64Bit)
            BinaryPrimitives.WriteUInt64BigEndian(data.Slice(offset, 8), value);
        else
            BinaryPrimitives.WriteUInt32BigEndian(data.Slice(offset, 4), (uint)value);
    }
}
