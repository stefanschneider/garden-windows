using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Containerizer.Controllers;

namespace Containerizer.Services.Implementations
{
    public class WsWriter : TextWriter
    {
        private readonly string streamName;
        private readonly WebSocketProxy ws;

        public WsWriter(string streamName, WebSocketProxy ws)
        {
            this.streamName = streamName;
            this.ws = ws;
        }

        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }

        public override void Write(string value)
        {
            ws.SendEvent(streamName, value + "\r\n");
        }
    }
}
