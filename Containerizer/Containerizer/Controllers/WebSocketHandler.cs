#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Containerizer.Models;
using Containerizer.Services.Implementations;
using IronFrame;
using Microsoft.Web.WebSockets;
using Newtonsoft.Json;
using Owin.WebSocket;
using Containerizer.Services.Interfaces;
using Logger;

#endregion

namespace Containerizer.Controllers
{
    public class WebSocketHandler : WebSocketConnection
    {
        private readonly IContainerService containerService;
        private readonly IRunService runService;
        private readonly ILogger logger;

        public WebSocketHandler(IContainerService containerService, IRunService runService, ILogger logger)
        {
            this.containerService = containerService;
            this.runService = runService;
            this.logger = logger;
        }

        public override void OnOpen()
        {
            var handle = Arguments["handle"];
            logger.Info("onOpen: {0}", handle);

            runService.container = containerService.GetContainerByHandle(handle);
        }

        public override void OnClose(WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            logger.Info("OnClose: {0} :: {1}", closeStatus.ToString(), closeStatusDescription);
        }

        public override void OnReceiveError(Exception error)
        {
            logger.Error("OnReceiveError: {0}", error.Message);
        }


        public override Task OnMessageReceived(ArraySegment<byte> message, WebSocketMessageType type)
        {
            var bytes = new UTF8Encoding(true).GetString(message.Array, 0, message.Count);
            var streamEvent = JsonConvert.DeserializeObject<ProcessStreamEvent>(bytes);

            if (streamEvent.MessageType == "run" && streamEvent.ApiProcessSpec != null)
            {
                runService.Run(new WebSocketProxy(this), streamEvent.ApiProcessSpec);
            }
            return null;
        }
    }

}