using MediaBrowser.Model.Plugins;
using System.Collections.Generic;
using Emby.MythTv.Model;

namespace Emby.MythTv.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string Host { get; set; }

        public string WebServiceUrl
        {
            get
            {
                return $"http://{Host}:6544";
            }
        }
        public bool LoadChannelIcons { get; set; }
        public bool UseSchedulesDirectImages { get; set; }
        public string Pin {get;set;}
        public List<StorageGroupMap> StorageGroupMaps { get; set; }
        public List<RecGroup> RecGroups { get; set; }

        public PluginConfiguration()
        {
            Host = "";
            Pin = "";
            LoadChannelIcons = false;
            UseSchedulesDirectImages = false;
            StorageGroupMaps = new List<StorageGroupMap>();
            RecGroups = new List<RecGroup>();
        }
    }
}
