#region

using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Helpers;
using Newtonsoft.Json;

#endregion

namespace Containerizer.Models
{
    public class ApiProcessSpec
    {
        public string Path { get; set; }
        public string[] Args { get; set; }
        public string[] Env { get; set; }
        [JsonProperty(PropertyName = "rlimits", NullValueHandling = NullValueHandling.Ignore)]
        public ResourceLimits Limits { get; set; }

        public string Arguments()
        {
            if (Args == null)
            {
                return null;
            }

            return ArgumentEscaper.Escape(Args);
        }
    }

    public class EnvironmentVariable {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class ResourceLimits
    {
        [JsonProperty(PropertyName = "nofile", NullValueHandling = NullValueHandling.Ignore)]
        public Int64 Nofile;
    }
}