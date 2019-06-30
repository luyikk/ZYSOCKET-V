using SpanJson;
using System;
using ZYSocket;
using ZYSocket.FiberStream;

namespace PlatformBenchmarks
{
    public partial class HttpHandler
    {

        public async void db(IFiberRw<HttpToken> fiberRw, WriteBytes write)
        {
            try
            {
                var data = await mPgsql.LoadSingleQueryRow();
                await JsonSerializer.NonGeneric.Utf8.SerializeAsync(data, write.Stream);
            }
            catch (Exception e_)
            {
                write.Write(e_.Message);
            }

            OnCompleted(fiberRw, write);
        }
    }
}
