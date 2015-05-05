#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web.Http;
using Containerizer.Controllers;
using Containerizer.Services.Interfaces;
using Moq;
using NSpec;
using IronFrame;
using System.Web.Http.Results;

#endregion

namespace Containerizer.Tests.Specs.Controllers
{
    internal class MetricsControllerSpec : nspec
    {
        private void describe_()
        {
            Mock<IContainerService> mockContainerService = null;
            Mock<IContainer> mockContainer = null;
            MetricsController metricsController = null;
            string containerHandle = null;
            const ulong privateBytes = 28;

            before = () =>
            {
                mockContainerService = new Mock<IContainerService>();
                mockContainer = new Mock<IContainer>();
                metricsController = new MetricsController(mockContainerService.Object)
                {
                    Configuration = new HttpConfiguration(),
                    Request = new HttpRequestMessage()
                };
                containerHandle = Guid.NewGuid().ToString();
             
                mockContainerService.Setup(x => x.GetContainerByHandle(containerHandle))
                        .Returns(() =>
                        {
                            return mockContainer != null ? mockContainer.Object : null;
                        });

                mockContainer.Setup(x => x.GetInfo()).Returns(new ContainerInfo
                {
                    MemoryStat = new ContainerMemoryStat
                    {
                        PrivateBytes = privateBytes
                    }
                });
            };

            describe[Controller.Show] = () =>
            {
                IHttpActionResult result = null;

                act = () => result = metricsController.Show(containerHandle);

                it["returns a successful status code"] = () =>
                {
                    result.VerifiesSuccessfulStatusCode();
                };

                it["returns the container metrics as a json"] = () =>
                {
                    var message = result.should_cast_to<JsonResult<ContainerMetrics>>();
                    message.Content.MemoryStat.PrivateBytes.should_be(privateBytes);
                };

                context["when the container does not exist"] = () =>
                {
                    before = () => mockContainer = null;

                    it["returns a 404"] = () =>
                    {
                        var message = result.should_cast_to<ResponseMessageResult>();
                        message.Response.StatusCode.should_be(HttpStatusCode.NotFound);
                    };
                };
            };

        }
    }
}