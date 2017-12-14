using MediaBrowser.Controller.LiveTv;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Emby.MythTv
{

    public interface IImageGrabber
    {
        Task<IEnumerable<ProgramInfo>> AddImages(IEnumerable<ProgramInfo> programs, CancellationToken cancellationToken);
    }
}
