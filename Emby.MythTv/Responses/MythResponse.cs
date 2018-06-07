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
    public class MythResponse
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

        // see https://github.com/MythTV/mythtv/blob/master/mythtv/libs/libmythbase/storagegroup.cpp#L23
        // and https://github.com/MythTV/mythtv/blob/master/mythtv/programs/mythbackend/mainserver.cpp#L4956
        private static List<string> specialGroups = new List<string>()
        {
            "DB Backups",
            "Videos",
            "Trailers",
            "Coverart",
            "Fanart",
            "Screenshots",
            "Banners",
            "Photographs",
            "Music",
            "MusicArt"
        };

        public List<StorageGroupDir> GetStorageGroupDirs(Stream stream, IJsonSerializer json, ILogger logger, bool excludeSpecial)
        {
            var root = json.DeserializeFromStream<RootStorageGroupDirList>(stream);
            var result = root.StorageGroupDirList.StorageGroupDirs;

            if (excludeSpecial)
                result.RemoveAll(g => specialGroups.Contains(g.GroupName));
            
            return result;
        }

        private class RootStorageGroupDirList
        {
            public StorageGroupDirList StorageGroupDirList { get; set; }
        }
    }
}
