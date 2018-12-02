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
            return inputStream.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int r = inputStream.Read(buffer, offset, count);
            if (r == 0)
            {
                await inputStream.Check();
                return inputStream.Read(buffer, offset, count);
            }

            return r;
        }


        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            outputStream.Write(buffer, offset, count);
            await outputStream.AwaitFlush();
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var task = Task.Run<int>(async () =>
              {
                  int r = inputStream.Read(buffer, offset, count);
                  if (r == 0)
                  {
                      await inputStream.Check();
                      return inputStream.Read(buffer, offset, count);
                  }
                  return r;
              });

            return TaskToApm.Begin(task, callback, state);

        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToApm.End<int>(asyncResult);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var task = Task.Run(async () =>
            {
                outputStream.Write(buffer, offset, count);
                await outputStream.AwaitFlush();
            });

            return TaskToApm.Begin(task, callback, state);

        }
        public override void EndWrite(IAsyncResult asyncResult)
        {
             TaskToApm.End(asyncResult);
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
            outputStream.Write(buffer, offset, count);
        }
    }

    public class MergeStreamAsyncResult : IAsyncResult
    {
        private readonly object asyncState;
        public object AsyncState => asyncState;

        private readonly EventWaitHandle eventWait;
        public WaitHandle AsyncWaitHandle => eventWait;

        private bool completedSynchronously;
        public bool CompletedSynchronously => completedSynchronously;

        private bool isCompleted;
        public bool IsCompleted => isCompleted;

        public MergeStreamAsyncResult(object asyncState)
        {
            eventWait = new EventWaitHandle(true, EventResetMode.ManualReset);
        }

        public void Completed()
        {
            isCompleted = true;
            eventWait.Set();           
        }

        public void Synchronously()
        {
            completedSynchronously = true;
        }
    }

}
