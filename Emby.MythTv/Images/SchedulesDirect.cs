using Emby.MythTv.Model;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Emby.MythTv
{

    public class SchedulesDirectImages : IImageGrabber
    {
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;

        private const string ApiUrl = "https://json.schedulesdirect.org/20141201";

        public SchedulesDirectImages(IHttpClient httpClient,
                                     IJsonSerializer jsonSerializer,
                                     ILogger logger)
        {
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }
        
        public async Task<IEnumerable<ProgramInfo>> AddImages(IEnumerable<ProgramInfo> programs, CancellationToken cancellationToken)
        {
            // get all images
            var progIds = programs.Select(p => p.ShowId).ToList();
            _logger.Info($"[MythTV] Fetching SchedulesDirect images");
            var images = await GetImageForPrograms(progIds, cancellationToken).ConfigureAwait(false);

            foreach (ProgramInfo program in programs)
            {
                if (images != null)
                {
                    var imageIndex = images.FindIndex(i => i.programID == program.ShowId.Substring(0, 10));
                    if (imageIndex > -1)
                    {
                        var allImages = (images[imageIndex].data ?? new List<ScheduleDirect.ImageData>()).ToList();
                        var imagesWithText = allImages.Where(i => string.Equals(i.text, "yes", StringComparison.OrdinalIgnoreCase)).ToList();
                        var imagesWithoutText = allImages.Where(i => string.Equals(i.text, "no", StringComparison.OrdinalIgnoreCase)).ToList();

                        double desiredAspect = 0.666666667;
                        double wideAspect = 1.77777778;

                        program.ImageUrl = GetProgramImage(ApiUrl, imagesWithText, true, desiredAspect) ??
                            GetProgramImage(ApiUrl, allImages, true, desiredAspect);

                        program.ThumbImageUrl = GetProgramImage(ApiUrl, imagesWithText, true, wideAspect);

                        // Don't supply the same image twice
                        if (string.Equals(program.ImageUrl, program.ThumbImageUrl, StringComparison.Ordinal))
                        {
                            program.ThumbImageUrl = null;
                        }

                        program.BackdropImageUrl = GetProgramImage(ApiUrl, imagesWithoutText, true, wideAspect);
                    }
                }
            }

            return programs;
        }

        private async Task<HttpResponseInfo> Post(HttpRequestOptions options,
                                                  bool enableRetry)
        {
            try
            {
                return await _httpClient.Post(options).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                if (!ex.StatusCode.HasValue || (int)ex.StatusCode.Value >= 500)
                {
                    enableRetry = false;
                }

                if (!enableRetry)
                {
                    throw;
                }
            }
            return await Post(options, false).ConfigureAwait(false);
        }

        private string GetProgramImage(string apiUrl, List<ScheduleDirect.ImageData> images, bool returnDefaultImage, double desiredAspect)
        {
            string url = null;

            var matches = images;

            matches = matches
                .OrderBy(i => Math.Abs(desiredAspect - GetAspectRatio(i)))
                .ThenByDescending(GetSizeOrder)
                .ToList();

            var match = matches.FirstOrDefault();

            if (match == null)
            {
                return null;
            }

            var uri = match.uri;

            if (!string.IsNullOrWhiteSpace(uri))
            {
                if (uri.IndexOf("http", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    url = uri;
                }
                else
                {
                    url = apiUrl + "/image/" + uri;
                }
            }
            //_logger.Debug("URL for image is : " + url);
            return url;
        }

        private int GetSizeOrder(ScheduleDirect.ImageData image)
        {
            if (!string.IsNullOrWhiteSpace(image.height))
            {
                int value;
                if (int.TryParse(image.height, out value))
                {
                    return value;
                }
            }

            return 0;
        }

        private double GetAspectRatio(ScheduleDirect.ImageData i)
        {
            int width = 0;
            int height = 0;

            if (!string.IsNullOrWhiteSpace(i.width))
            {
                int.TryParse(i.width, out width);
            }

            if (!string.IsNullOrWhiteSpace(i.height))
            {
                int.TryParse(i.height, out height);
            }

            if (height == 0 || width == 0)
            {
                return 0;
            }

            double result = width;
            result /= height;
            return result;
        }

        private async Task<List<ScheduleDirect.ShowImages>> GetImageForPrograms(
                                                                                List<string> programIds,
                                                                                CancellationToken cancellationToken)
        {
            if (programIds.Count == 0)
            {
                return new List<ScheduleDirect.ShowImages>();
            }

            var imageIdString = "[";

            foreach (var i in programIds)
            {
                var imageId = i.Substring(0, 10);

                if (!imageIdString.Contains(imageId))
                {
                    imageIdString += "\"" + imageId + "\",";
                }
            }

            imageIdString = imageIdString.TrimEnd(',') + "]";

            var httpOptions = new HttpRequestOptions()
                {
                    Url = ApiUrl + "/metadata/programs",
                    UserAgent = "Emby",
                    CancellationToken = cancellationToken,
                    RequestContent = imageIdString,
                    LogErrorResponseBody = true,
                    // The data can be large so give it some extra time
                    TimeoutMs = 60000
                };

            try
            {
                using (var innerResponse2 = await Post(httpOptions, true).ConfigureAwait(false))
                {
                    return _jsonSerializer.DeserializeFromStream<List<ScheduleDirect.ShowImages>>(
                                                                                                  innerResponse2.Content);
                }
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error getting image info from schedules direct", ex);

                return new List<ScheduleDirect.ShowImages>();
            }
        }
    }

}
