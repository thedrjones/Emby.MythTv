using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emby.MythTv.Model
{
    public class BuildInfo
    {
        public string Version { get; set; }
        public bool LibX264 { get; set; }
        public bool LibDNS_SD { get; set; }
    }
}
