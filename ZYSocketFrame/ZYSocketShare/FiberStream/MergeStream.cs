using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ZYSocket.FiberStream
{
    public class MergeStream : Stream
    {
        private readonly IFiberReadStream inputStream;
        private readonly IFiberWriteStream outputStream;

        public MergeStream(IFiberReadStream input,IFiberWriteStream output)
        {
            inputStream = input;
            outputStream = output;
        }

        public bool IsSync { get; set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            outputStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {

            if (IsSync)
            {
                int r = inputStream.Read(buffer, offset, count);

                if (r == 0)
                {
                    inputStream.Check().Wait();
                    r = inputStream.Read(buffer, offset, count);
                }
                return r;
            }else
                return inputStream.Read(buffer, offset, count);

        }

       

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (IsSync)
            {
                outputStream.Write(buffer, offset, count);
                outputStream.Flush();
            }else
                outputStream.Write(buffer, offset, count);
        }
    }
}
