using System;
using System.Globalization;
using ZYSocket;
using ZYSocket.FiberStream;

namespace PlatformBenchmarks
{
    public partial class HttpHandler
    {

        private readonly static AsciiString _fortunesTableStart = "<!DOCTYPE html><html><head><title>Fortunes</title></head><body><table><tr><th>id</th><th>message</th></tr>";
        private readonly static AsciiString _fortunesRowStart = "<tr><td>";
        private readonly static AsciiString _fortunesColumn = "</td><td>";
        private readonly static AsciiString _fortunesRowEnd = "</td></tr>";
        private readonly static AsciiString _fortunesTableEnd = "</table></body></html>";

        public async void fortunes(IFiberRw<HttpToken> fiberRw, WriteBytes write)
        {
            try
            {
                var data = await mPgsql.LoadFortunesRows();
                write.Write(_fortunesTableStart.Data, 0, _fortunesTableStart.Length);
                foreach (var item in data)
                {
                    write.Write(_fortunesRowStart.Data, 0, _fortunesRowStart.Length);
                    write.Write(item.Id.ToString(CultureInfo.InvariantCulture));
                    write.Write(_fortunesColumn.Data, 0, _fortunesColumn.Length);
                    write.Write(System.Web.HttpUtility.HtmlEncode(item.Message));
                    write.Write(_fortunesRowEnd.Data, 0, _fortunesRowEnd.Length);
                }
                write.Write(_fortunesTableEnd.Data, 0, _fortunesTableEnd.Length);
            }
            catch (Exception e_)
            {
                write.Write(e_.Message);
            }
            OnCompleted(fiberRw, write);
        }
    }
}
