/***************************************************************************
 *  IPodPlaylistSource.cs
 *
 *  Copyright (C) 2007 Kevin Kubasik
 *  Written by Kevin Kubasik <kevin@kubasik.net>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using Banshee.Dap;
using Banshee.Sources;
using Banshee.Base;
using IPod;


namespace Banshee.Dap.Ipod
{
	
	public class IPodPlaylistSource : DapPlaylistSource
	{
	    protected IpodDap device;
	    
		public IPodPlaylistSource(IpodDap device, string name) : base(device, name)
        {
            this.device = device;
        }
        
        public override void AddTrack(TrackInfo track)
        {
            if (track == null) {
            	return;
            }
            
            IpodDapTrackInfo new_track = null;

            if(track is IpodDapTrackInfo) {
                new_track = track as IpodDapTrackInfo;
            } else {
                new_track = new IpodDapTrackInfo(track, device.Device.TrackDatabase);
            }
            
            base.AddTrack(new_track);            
        }
        
        public override void SourceDrop(Banshee.Sources.Source source)
        {
            if(source == this || source == null ) {
                return;
            }
            
            if (source.Tracks == null) {
            	return;
            }
            	
            LogCore.Instance.PushDebug("In IPodPlaylistSource.SourceDrop" , "");
            
            foreach(TrackInfo ti in source.Tracks) {
                LogCore.Instance.PushDebug("Adding track " + ti.ToString() , " to playlist " + source.Name);
                IpodDapTrackInfo idti = new IpodDapTrackInfo(ti, device.Device.TrackDatabase);
                AddTrack(idti);
            }
        }
        
        public override bool AcceptsInput {
            get { return true; }
        }
        
        public override bool Unmap()
        {
            LogCore.Instance.PushDebug("In IPodPlaylistSource.UnMap" , "");
            
            if(Count > 0 && !ConfirmUnmap(this)) {
                return false;
            }
                        
            foreach (IpodDapTrackInfo idti in Tracks) {
                LogCore.Instance.PushDebug("Trying to remove track from ipod source" , "Track: " + idti.ToString());
                device.RemoveTrackIfNotInPlaylists(idti, this);
            }
            tracks.Clear();
            
            //SourceManager.RemoveSource(this);
            device.Source.RemoveChildSource(this);
            
            IPod.Playlist p = device.Device.TrackDatabase.LookupPlaylist(Name);
            if (p != null) {
                LogCore.Instance.PushDebug("Removing playlist from ipod." , "");                    
                device.Device.TrackDatabase.RemovePlaylist(p);
               
                IPod.Playlist tempPlaylist = device.Device.TrackDatabase.LookupPlaylist(Name);
                if (tempPlaylist == null) {
                    LogCore.Instance.PushDebug("Removing playlist from ipod succeeded." , "");
                } else {
                    LogCore.Instance.PushDebug("Removing playlist from ipod failed." , "");
                }
            }
           
            return true;
        }
        
        /*
        // For use if we decide to prevent the user from removing the "podcast" playlist.
        protected void ShowPodcastPlaylistUnmapWarningMsg() 
        {
            HigMessageDialog dialog = new HigMessageDialog(null, Gtk.DialogFlags.Modal,
                Gtk.MessageType.Warning, Gtk.ButtonsType.Ok,
                Catalog.GetString("The Podcast Playlist Can Not Be Deleted"),
                Catalog.GetString("The Podcast Playlist is a special playlist, which can not be deleted.\n"));                                    
            try {
                dialog.Run();                
            } finally {
                dialog.Destroy();
            }
        }
        
        // For use if we decide to prevent the user from using the "podcast" name for a new playlist.
        protected void ShowPodcastPlaylistCreateWarningMsg() 
        {
            HigMessageDialog dialog = new HigMessageDialog(null, Gtk.DialogFlags.Modal,
                Gtk.MessageType.Warning, Gtk.ButtonsType.Ok,
                Catalog.GetString("The \"Podcast\" Name Can Not Be Used"),
                Catalog.GetString("The Podcast Playlist is a special playlist.  " +
                    "The \"Podcast\" playlist name can not be used as a name for a playlist.\n"));
            try {
                dialog.Run();                
            } finally {
                dialog.Destroy();
            }
        }
        */
 
	}
}


