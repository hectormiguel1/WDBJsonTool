using System.Buffers.Binary;

namespace WDBJsonTool.Common.FieldProcessors;

/// <summary>
/// Processes 32-bit or 64-bit unsigned integer fields.
/// </summary>
public class UIntFieldProcessor : IFieldProcessor<ulong>
{
    private readonly bool _is64Bit;

    public int SizeInBytes => _is64Bit ? 8 : 4;

    public UIntFieldProcessor(bool is64Bit = false)
    {
        _is64Bit = is64Bit;
    }

    public ulong Extract(ReadOnlySpan<byte> data, int offset)
    {
        if (_is64Bit)
        {
            return BinaryPrimitives.ReadUInt64BigEndian(data.Slice(offset, 8));
        }
        else
        {
            return BinaryPrimitives.ReadUInt32BigEndian(data.Slice(offset, 4));
        }
    }

    public void Write(Span<byte> data, int offset, ulong value)
    {
        if (_is64Bit)
        {
            BinaryPrimitives.WriteUInt64BigEndian(data.Slice(offset, 8), value);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(data.Slice(offset, 4), (uint)value);
        }
    }
}
