﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Logging;
using MediaBrowser.Controller.LiveTv;
using System.Linq;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common;
using MediaBrowser.Model.Dto;
using System.Globalization;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.LiveTv;

namespace Emby.MythTv
{
    public class RecordingsChannel : IChannel, IHasCacheKey, ISupportsDelete, ISupportsLatestMedia, ISupportsMediaProbe, IHasFolderAttributes, IHasChangeEvent, IDisposable
    {
        public ILiveTvManager _liveTvManager;
        public readonly ILogger _logger;

        public event EventHandler ContentChanged;

        private Timer _updateTimer;

        public void OnContentChanged()
        {
            if (ContentChanged != null)
            {
                ContentChanged(this, EventArgs.Empty);
            }
        }

        private void OnUpdateTimerCallback(object state)
        {
            OnContentChanged();
        }



        public RecordingsChannel(ILiveTvManager liveTvManager, ILogger logger)
        {
            _liveTvManager = liveTvManager;
            _logger = logger;
            _logger.Info("[MythTV] Initiating RecordingsChannel");

            var interval = TimeSpan.FromMinutes(15);
            _updateTimer = new Timer (OnUpdateTimerCallback,null, interval, interval);
        }

        public void Dispose()
        {
            if (_updateTimer !=null)
            {
                _updateTimer.Dispose();
                _updateTimer = null;
            }
        }

        public string Name
        {
            get
            {
                return "MythTV Recordings";
            }
        }

        public string Description
        {
            get
            {
                return "MythTV Recordings";
            }
        }

        public string[] Attributes
        {
            get
            {
                return new[] { "Recordings" };
            }
        }

        public string DataVersion
        {
            get
            {
                return "1";
            }
        }

        public string HomePageUrl
        {
            get { return "https://www.mythtv.org/"; }
        }

        public ChannelParentalRating ParentalRating
        {
            get { return ChannelParentalRating.GeneralAudience; }
        }

        public string GetCacheKey(string userId)
        {
            var now = DateTime.UtcNow;

            var values = new List<string>();

            values.Add(now.DayOfYear.ToString(CultureInfo.InvariantCulture));
            values.Add(now.Hour.ToString(CultureInfo.InvariantCulture));

            double minute = now.Minute;
            minute /= 5;

            values.Add(Math.Floor(minute).ToString(CultureInfo.InvariantCulture));

            values.Add(GetService().LastRecordingChange.Ticks.ToString(CultureInfo.InvariantCulture));

            return string.Join("-", values.ToArray());
        }

        public InternalChannelFeatures GetChannelFeatures()
        {
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Movie,
                    ChannelMediaContentType.Episode,
                    ChannelMediaContentType.Clip
                },
                MediaTypes = new List<ChannelMediaType>
                {
                    ChannelMediaType.Audio,
                    ChannelMediaType.Video
                },
                SupportsContentDownloading = true
            };
        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            if (type == ImageType.Primary)
            {
                return Task.FromResult(new DynamicImageResponse
                        {
                            Path = "https://www.mythtv.org/img/mythtv.png",
                            Protocol = MediaProtocol.Http,
                            HasImage = true
                        });
            }

            return Task.FromResult(new DynamicImageResponse
                    {
                        HasImage = false
                    });
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return new List<ImageType>
            {
                ImageType.Primary
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true;
        }

        private LiveTvService GetService()
        {
            return _liveTvManager.Services.OfType<LiveTvService>().First();
        }

        public bool CanDelete(BaseItem item)
        {
            return !item.IsFolder;
        }

        public Task DeleteItem(string id, CancellationToken cancellationToken)
        {
            return GetService().DeleteRecordingAsync(id, cancellationToken);
        }

        public Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(query.FolderId))
            {
                return GetRecordingGroups(query, cancellationToken);
            }

            if (query.FolderId.StartsWith("series_", StringComparison.OrdinalIgnoreCase))
            {
                var hash = query.FolderId.Split('_')[1];
                return GetChannelItems(query, i => i.IsSeries && string.Equals(i.Name.GetMD5().ToString("N"), hash, StringComparison.Ordinal), cancellationToken);
            }

            if (string.Equals(query.FolderId, "kids", StringComparison.OrdinalIgnoreCase))
            {
                return GetChannelItems(query, i => i.IsKids, cancellationToken);
            }

            if (string.Equals(query.FolderId, "movies", StringComparison.OrdinalIgnoreCase))
            {
                return GetChannelItems(query, i => i.IsMovie, cancellationToken);
            }

            if (string.Equals(query.FolderId, "news", StringComparison.OrdinalIgnoreCase))
            {
                return GetChannelItems(query, i => i.IsNews, cancellationToken);
            }

            if (string.Equals(query.FolderId, "sports", StringComparison.OrdinalIgnoreCase))
            {
                return GetChannelItems(query, i => i.IsSports, cancellationToken);
            }

            if (string.Equals(query.FolderId, "others", StringComparison.OrdinalIgnoreCase))
            {
                return GetChannelItems(query, i => !i.IsSports && !i.IsNews && !i.IsMovie && !i.IsKids && !i.IsSeries, cancellationToken);
            }

            var result = new ChannelItemResult()
                {
                    Items = new List<ChannelItemInfo>()
                };

            return Task.FromResult(result);
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, Func<RecordingInfo, bool> filter, CancellationToken cancellationToken)
        {
            var service = GetService();
            var allRecordings = await service.GetRecordingsAsync(cancellationToken).ConfigureAwait(false);

            var result = new ChannelItemResult()
                {
                    Items = new List<ChannelItemInfo>()
                };

            result.Items.AddRange(allRecordings.Where(filter).Select(ConvertToChannelItem));

            return result;
        }

        private ChannelItemInfo ConvertToChannelItem(RecordingInfo item)
        {
            var path = string.IsNullOrEmpty(item.Path) ? item.Url : item.Path;

            var channelItem = new ChannelItemInfo
            {
                Name = string.IsNullOrEmpty(item.EpisodeTitle) ? item.Name : item.EpisodeTitle,
                SeriesName = !string.IsNullOrEmpty(item.EpisodeTitle) || item.IsSeries ? item.Name : null,
                OfficialRating = item.OfficialRating,
                CommunityRating = item.CommunityRating,
                ContentType = item.IsMovie ? ChannelMediaContentType.Movie : (item.IsSeries ? ChannelMediaContentType.Episode : ChannelMediaContentType.Clip),
                Genres = item.Genres,
                ImageUrl = item.ImageUrl,
                //HomePageUrl = item.HomePageUrl
                Id = item.Id,
                //IndexNumber = item.IndexNumber,
                MediaType = item.ChannelType == ChannelType.TV ? ChannelMediaType.Video : ChannelMediaType.Audio,
                MediaSources = new List<MediaSourceInfo>
                {
                    new MediaSourceInfo
                    {
                        Path = path,
                        Protocol = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? MediaProtocol.Http : MediaProtocol.File,
                        Id = item.Id,
                        IsInfiniteStream = item.Status == RecordingStatus.InProgress,
                        RunTimeTicks = (item.EndDate - item.StartDate).Ticks
                    }
                },
                //ParentIndexNumber = item.ParentIndexNumber,
                PremiereDate = item.OriginalAirDate,
                //ProductionYear = item.ProductionYear,
                //Studios = item.Studios,
                Type = ChannelItemType.Media,
                DateModified = item.DateLastUpdated,
                Overview = item.Overview,
                //People = item.People
                IsLiveStream = item.Status == RecordingStatus.InProgress,
                Etag = item.Status.ToString()
            };

            return channelItem;
        }

        private async Task<ChannelItemResult> GetRecordingGroups(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            var service = GetService();

            var allRecordings = await service.GetRecordingsAsync(cancellationToken).ConfigureAwait(false);
            var result = new ChannelItemResult()
                {
                    Items = new List<ChannelItemInfo>()
                };

            var series = allRecordings
                .Where(i => i.IsSeries)
                .ToLookup(i => i.Name, StringComparer.OrdinalIgnoreCase);

            result.Items.AddRange(series.OrderBy(i => i.Key).Select(i => new ChannelItemInfo
                    {
                        Name = i.Key,
                        FolderType = ChannelFolderType.Container,
                        Id = "series_" + i.Key.GetMD5().ToString("N"),
                        Type = ChannelItemType.Folder,
                        ImageUrl = i.First().ImageUrl
                    }));

            var kids = allRecordings.FirstOrDefault(i => i.IsKids);

            if (kids != null)
            {
                result.Items.Add(new ChannelItemInfo
                        {
                            Name = "Kids",
                            FolderType = ChannelFolderType.Container,
                            Id = "kids",
                            Type = ChannelItemType.Folder,
                            ImageUrl = kids.ImageUrl
                        });
            }

            var movies = allRecordings.FirstOrDefault(i => i.IsMovie);
            if (movies != null)
            {
                result.Items.Add(new ChannelItemInfo
                        {
                            Name = "Movies",
                            FolderType = ChannelFolderType.Container,
                            Id = "movies",
                            Type = ChannelItemType.Folder,
                            ImageUrl = movies.ImageUrl
                        });
            }

            var news = allRecordings.FirstOrDefault(i => i.IsNews);
            if (news != null)
            {
                result.Items.Add(new ChannelItemInfo
                        {
                            Name = "News",
                            FolderType = ChannelFolderType.Container,
                            Id = "news",
                            Type = ChannelItemType.Folder,
                            ImageUrl = news.ImageUrl
                        });
            }

            var sports = allRecordings.FirstOrDefault(i => i.IsSports);
            if (sports != null)
            {
                result.Items.Add(new ChannelItemInfo
                        {
                            Name = "Sports",
                            FolderType = ChannelFolderType.Container,
                            Id = "sports",
                            Type = ChannelItemType.Folder,
                            ImageUrl = sports.ImageUrl
                        });
            }

            var other = allRecordings.FirstOrDefault(i => !i.IsSports && !i.IsNews && !i.IsMovie && !i.IsKids && !i.IsSeries);
            if (other != null)
            {
                result.Items.Add(new ChannelItemInfo
                        {
                            Name = "Others",
                            FolderType = ChannelFolderType.Container,
                            Id = "others",
                            Type = ChannelItemType.Folder,
                            ImageUrl = other.ImageUrl
                        });
            }

            return result;
        }
    }
}
