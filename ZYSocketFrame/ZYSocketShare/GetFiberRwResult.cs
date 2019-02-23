using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZYSocket
{
    public class GetFiberRwResult
    {
       
        public Stream Input { get; set; }
        public Stream Output { get; set; }

        public GetFiberRwResult(Stream input, Stream output)
        {
            Input = input;
            Output = output;
        }
    }
}
