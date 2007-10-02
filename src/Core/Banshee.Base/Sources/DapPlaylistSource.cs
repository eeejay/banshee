using System;
using System.Collections.Generic;
using Banshee.Sources;
using Banshee.Base;
using Banshee.Dap;

namespace Banshee.Base
{
    public class DapPlaylistSource : AbstractPlaylistSource
    {
        private DapDevice device;
        public DapPlaylistSource(DapDevice device, string name) : base(name)
        {
            this.device = device;
        }

        public override void AddTrack(TrackInfo track)
        {
            if (track == null) {
            	return;
            }
            
            if (!base.ContainsTrack(track)) {
                base.AddTrack(track);
            }
            
            device.AddTrack(track);
        }
        
        public override void RemoveTrack(TrackInfo track)
        {
            base.RemoveTrack(track);
            device.RemoveTrack(track);
        }
    }
}
