using System;
using System.Collections.Generic;
using Moq;
using NSpec;
using Containerizer.Models;
using IronFrame;
using Containerizer.Services.Interfaces;
using Containerizer.Services.Implementations;

namespace Containerizer.Tests.Specs.Services
{
    internal class ContainerInfoServiceSpec : nspec
    {
        private void describe_()
        {
            Mock<IContainerService> mockContainerService = null;
            string handle = "container-handle";
            ContainerInfoService service = null;
            Mock<IContainer> mockContainer = null;
            Mock<IExternalIP>  mockExternalIP = null;

            before = () =>
            {
                mockContainer = new Mock<IContainer>();
                mockContainerService = new Mock<IContainerService>();
                mockContainerService.Setup(x => x.GetContainerByHandle(handle))
                    .Returns(mockContainer.Object);
                mockExternalIP = new Mock<IExternalIP>();
                service = new ContainerInfoService(mockContainerService.Object, mockExternalIP.Object);
            };

            describe["GetInfoByHandle"] = () =>
            {
                ContainerInfoApiModel result = null;
                int expectedHostPort = 1337;
                string expectedExternalIP = "10.11.12.13";

                before = () =>
                {
                    mockContainer.Setup(x => x.GetInfo()).Returns(new ContainerInfo
                    {
                        ReservedPorts = new List<int> { expectedHostPort },
                        Properties = new Dictionary<string, string>
                        {
                            {"Keymaster", "Gatekeeper"}
                        }
                    });
                    mockExternalIP.Setup(x => x.ExternalIP()).Returns(expectedExternalIP);
                };

                act = () =>
                {
                    result = service.GetInfoByHandle(handle);
                };

                it["returns info about the container"] = () =>
                {
                    var portMapping = result.MappedPorts[0];
                    portMapping.HostPort.should_be(expectedHostPort);
                    portMapping.ContainerPort.should_be(8080);
                };

                it["returns container properties"] = () =>
                {
                    var properties = result.Properties;
                    properties["Keymaster"].should_be("Gatekeeper");
                };

                it["returns the external ip address"] = () =>
                {
                    var extrernalIP = result.ExternalIP;
                    extrernalIP.should_be(expectedExternalIP);
                };

                context["when the container does not exist"] = () =>
                {
                    before = () => mockContainerService.Setup(x => x.GetContainerByHandle(handle)).Returns(null as IContainer);

                    it["returns not found"] = () =>
                    {
                        result.should_be_null();
                    };
                };
            };

            describe["GetMetricsByHandle"] = () =>
            {
                ContainerMetricsApiModel result = null;
                const ulong privateBytes = 7654;
                const ulong cpuUsage = 4321;

                before = () => mockContainer.Setup(x => x.GetInfo()).Returns(new ContainerInfo
                {
                    MemoryStat = new ContainerMemoryStat
                    {
                        PrivateBytes = privateBytes
                    },
                    CpuStat = new ContainerCpuStat
                    {
                        TotalProcessorTime = new TimeSpan(0,0,0,0,(int)cpuUsage)
                    }
                });

                 act = () => result = service.GetMetricsByHandle(handle);

                it["returns memory metrics about the container"] = () =>
                {
                    result.MemoryStat.TotalBytesUsed.should_be(privateBytes);
                };

                it["returns cpu usage metrics about the container"] = () =>
                {
                    result.CPUStat.Usage.should_be(cpuUsage);
                };

                context["when the container does not exist"] = () =>
                {
                    before = () => mockContainerService.Setup(x => x.GetContainerByHandle(handle)).Returns(null as IContainer);

                    it["returns not found"] = () =>
                    {
                        result.should_be_null();
                    };
                };
            };
        }
    }
}