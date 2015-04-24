using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Containerizer.Controllers;
using Containerizer.Models;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;
using Owin.WebSocket;

namespace Containerizer.Services.Implementations
{
    public class WebSocketProxy : IProcessProxy
    {
        private WebSocketConnection ws;
        public TextWriter StandardOutput { get; set; }
        public TextWriter StandardError { get; set; }
        public TextReader StandardInput { get; set; }


        public WebSocketProxy(WebSocketConnection ws)
        {
            this.ws = ws;
            this.StandardOutput = new  WsWriter("stdout", this);
            this.StandardError = new  WsWriter("stderr", this);
        }

        public void SendEvent(string messageType, string message)
        {
            // logger.Info("SendEvent: {0} :: {1}", messageType, message);
            var jsonString = JsonConvert.SerializeObject(new ProcessStreamEvent
            {
                MessageType = messageType,
                Data = message
            }, Formatting.None);
            var data = new UTF8Encoding(true).GetBytes(jsonString);
            ws.SendText(data, true);
        }

        public void SetProcessPid(int pid)
        {
            SendEvent("pid", pid.ToString());
        }

        public void ProcessExited(int exitCode)
        {
            SendEvent("close", exitCode.ToString());
            ws.Close(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "process finished");
        }

        public void ProcessExitedWithError(Exception ex)
        {
            SendEvent("close", "-1");
            ws.Close(System.Net.WebSockets.WebSocketCloseStatus.InternalServerError, ex.Message);
        }
    }
}
