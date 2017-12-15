using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Emby.MythTv.Helpers;
using Emby.MythTv.Model;

namespace Emby.MythTv.Responses
{
    public class UtilityResponse
    {
        public static string GetVersion(Stream stream, IJsonSerializer json, ILogger logger)
        {
            var root = json.DeserializeFromStream<RootBackendInfoObject>(stream);
            return root.BackendInfo.Build.Version;
        }

        private class RootBackendInfoObject
        {
            public BackendInfo BackendInfo { get; set; }
        }
    }
}
