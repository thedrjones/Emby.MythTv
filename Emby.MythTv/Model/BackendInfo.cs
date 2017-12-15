using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emby.MythTv.Model
{
    public class BackendInfo
    {
        public BuildInfo Build { get; set; }
        public EnvInfo Env { get; set; }
        public LogInfo Log { get; set; }
    }
}
