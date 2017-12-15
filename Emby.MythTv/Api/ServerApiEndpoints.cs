using MediaBrowser.Controller.Net;
using System;
using System.Linq;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Logging;
using MediaBrowser.Common.Net;
using Emby.MythTv.Responses;

namespace Emby.MythTv.Api
{
    [Route("/MythTV/GetStorageGroups", "GET", Summary = "Returns MythTV storage groups")]
    public class GetStorageGroups : IReturn<string>
    {
    }

    class ServerApiEndpoints : IService
    {

        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;

        public ServerApiEndpoints(ILogManager logManager, IHttpClient httpClient)
        {
            _logger = logManager.GetLogger(GetType().Name);
            _httpClient = httpClient;
        }
        
        
        public object Get(GetStorageGroups request)
        {
            return "teststring";
        }

    }
}
                
