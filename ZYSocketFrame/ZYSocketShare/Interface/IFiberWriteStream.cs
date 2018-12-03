using System.Threading.Tasks;

namespace ZYSocket.FiberStream
{
    public interface IFiberWriteStream
    {

        byte[] Numericbytes { get; }

        long Length { get; }
        long Position { get; set; }
        void Flush();
        ValueTask<int> AwaitFlush();
        void SetLength(long value);
        void Write(byte[] buffer, int offset, int count);
        void Close();

    }
}