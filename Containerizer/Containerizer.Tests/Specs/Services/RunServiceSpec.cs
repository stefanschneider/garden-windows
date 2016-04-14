﻿#region

using System;
using Containerizer.Services.Implementations;
using NSpec;
using IronFrame;
using Moq;
using Containerizer.Controllers;
using Containerizer.Models;
using ContainerInfo = IronFrame.ContainerInfo;

#endregion

namespace Containerizer.Tests.Specs.Services
{
    internal class RunServiceSpec : nspec
    {
        private void describe_Run()
        {
            Mock<IWebSocketEventSender> websocketMock = null;
            Mock<IContainer> containerMock = null;
            Mock<IContainerProcess> processMock = null;
            RunService runService = null;

            before = () =>
            {
                websocketMock = new Mock<IWebSocketEventSender>();
                containerMock = new Mock<IContainer>();
                processMock = new Mock<IContainerProcess>();

                runService = new RunService();
                runService.container = containerMock.Object;

                containerMock.Setup(x => x.Directory).Returns(new Mock<IContainerDirectory>().Object);
            };

            context["#Run"] = () =>
            {
                before = () => {
                    processMock.Setup(x => x.Id).Returns(5432);
                    containerMock.Setup(x => x.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>())).Returns(processMock.Object);
                };

                act = () => runService.Run(websocketMock.Object, new ApiProcessSpec());

                it["sends the process pid on the websocket"] = () =>
                {
                    websocketMock.Verify(x => x.SendEvent("pid", "5432"));
                };
            };

            context["Process exits normally"] = () =>
            {
                before = () => containerMock.Setup(x => x.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>())).Returns(processMock.Object);

                context["Process exits with 1"] = () =>
                {
                    before = () => processMock.Setup(x => x.WaitForExit()).Returns(1);

                    it["sends a close event with data == '1'"] = () =>
                    {
                        var apiProcessSpec = new ApiProcessSpec();
                        runService.Run(websocketMock.Object, apiProcessSpec);

                        websocketMock.Verify(x => x.SendEvent("close", "1"));
                    };
                };

                context["Process exits with 0"] = () =>
                {
                    before = () => processMock.Setup(x => x.WaitForExit()).Returns(0);

                    it["sends a close event with data == '0'"] = () =>
                    {
                        var apiProcessSpec = new ApiProcessSpec();
                        runService.Run(websocketMock.Object, apiProcessSpec);

                        websocketMock.Verify(x => x.SendEvent("close", "0"));
                    };
                };
            };

            context["Process throws an exception while running"] = () =>
            {
                before = () =>
                {
                    containerMock.Setup(x => x.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>()))
                        .Returns(processMock.Object);
                    processMock.Setup(x => x.WaitForExit()).Throws(new Exception("Running is hard"));
                };

                it["sends a close event with data == '-1'"] = () =>
                {
                    var apiProcessSpec = new ApiProcessSpec();
                    runService.Run(websocketMock.Object, apiProcessSpec);

                    websocketMock.Verify(x => x.SendEvent("close", "-1"));
                };
            };

            context["container#run throws an exception"] = () =>
            {
                before = () => containerMock.Setup(x => x.Run(It.IsAny<ProcessSpec>(), It.IsAny<IProcessIO>())).Throws(new Exception("filename doesn't exist"));

                it["sends an error event"] = () =>
                {
                    var apiProcessSpec = new ApiProcessSpec();
                    runService.Run(websocketMock.Object, apiProcessSpec);

                    websocketMock.Verify(x => x.SendEvent("error", "filename doesn't exist"));
                    websocketMock.Verify(x => x.Close(System.Net.WebSockets.WebSocketCloseStatus.InternalServerError, "filename doesn't exist"));
                };
            };

            describe["environment variables"] = () =>
            {
                it["passes processSpec.Env environment variables to the process"] = () =>
                {
                    containerMock.Setup(x => x.GetInfo()).Returns(new ContainerInfo());
                    runService.Run(websocketMock.Object, new ApiProcessSpec
                    {
                        Env = new string[] { "foo=bar", "jane=jill=jim" },
                    });
                    containerMock.Verify(x => x.Run(It.Is((ProcessSpec p) => p.Environment["foo"] == "bar" && p.Environment["jane"] == "jill=jim"), It.IsAny<IProcessIO>()));
                };

                it["kv pairs from processSpec.Env override a pairs from executor:env"] = () =>
                {
                    containerMock.Setup(x => x.GetInfo()).Returns(new ContainerInfo
                    {
                        Properties = new System.Collections.Generic.Dictionary<string, string> { { "executor:env", "[{\"name\":\"FOO\",\"value\":\"Bar\"}]" } }
                    });
                    runService.Run(websocketMock.Object, new ApiProcessSpec
                    {
                        Env = new string[] { "FOO=Baz" },
                    });
                    containerMock.Verify(x => x.Run(It.Is((ProcessSpec p) => p.Environment["FOO"] == "Baz"), It.IsAny<IProcessIO>()));
                };

                it["sets ENV[USERPROFILE]"] = () =>
                {
                    containerMock.Setup(x => x.Directory.UserPath).Returns(@"E:\Fred");

                    runService.Run(websocketMock.Object, new ApiProcessSpec());
                    containerMock.Verify(x => x.Run(It.Is((ProcessSpec p) => p.Environment["USERPROFILE"] == @"E:\Fred"), It.IsAny<IProcessIO>()));
                };

                context["when no container port is in the request"] = () =>
                {
                    it["ENV[PORT] remains unset"] = () =>
                    {
                        containerMock.Setup(x => x.GetInfo()).Returns(new ContainerInfo
                        {
                            ReservedPorts = new System.Collections.Generic.List<int> {1234},
                        });

                        runService.Run(websocketMock.Object, new ApiProcessSpec());
                        containerMock.Verify(
                            x =>
                                x.Run(It.Is((ProcessSpec p) => !p.Environment.ContainsKey("PORT")), It.IsAny<IProcessIO>()));
                    };
                };

            };
        }

    }
}