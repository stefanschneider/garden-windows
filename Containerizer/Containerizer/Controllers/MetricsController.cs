#region

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Containerizer.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using IronFrame;
using System.Collections.Generic;
using System.Diagnostics;

#endregion

namespace Containerizer.Controllers
{
    public class MetricsController : ApiController
    {
        private readonly IContainerService _containerService;

        public MetricsController(IContainerService containerService)
        {
            this._containerService = containerService;
        }

        [Route("api/containers/{handle}/metrics")]
        [HttpGet]
        public IHttpActionResult Show(string handle)
        {
            var container = _containerService.GetContainerByHandle(handle);

            if (container == null)
                return
                    ResponseMessage(Request.CreateResponse(System.Net.HttpStatusCode.NotFound,
                        string.Format("container does not exist: {0}", handle)));

            var info = container.GetInfo();
            var metrics = new ContainerMetrics
            {
                MemoryStat = info.MemoryStat
            };

            return Json(metrics);
        }
    }

    public class ContainerMetrics
    {
        public ContainerMemoryStat MemoryStat { get; set; }
    }
}