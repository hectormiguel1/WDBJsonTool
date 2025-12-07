using System.Buffers.Binary;

namespace WDBJsonTool.Common.FieldProcessors;

/// <summary>
/// Processes 32-bit IEEE 754 floating-point fields.
/// </summary>
public class FloatFieldProcessor : IFieldProcessor<float>
{
    public int SizeInBytes => 4;

    public float Extract(ReadOnlySpan<byte> data, int offset)
    {
        return BinaryPrimitives.ReadSingleBigEndian(data.Slice(offset, 4));
    }

    public void Write(Span<byte> data, int offset, float value)
    {
        BinaryPrimitives.WriteSingleBigEndian(data.Slice(offset, 4), value);
    }
}
