/***************************************************************************
 *  StationStore.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
using System.IO;
using System.Collections;
using Gtk;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Playlists.Formats.Xspf;
 
namespace Banshee.Plugins.Radio
{
    public class StationModel : TreeStore
    {
        private RadioPlugin plugin;
        
        public event EventHandler Reloaded;
        
        public StationModel(RadioPlugin plugin) : base( 
            typeof(string), typeof(string), typeof(Track), typeof(RadioTrackInfo))
        {
            this.plugin = plugin;
            plugin.StationManager.StationsLoaded += OnStationsLoaded;
        }
        
        private void OnStationsLoaded(object o, EventArgs args)
        {
            Clear();
            
            foreach(Playlist station_group in plugin.StationManager.StationGroups) {
                AddStationGroup(station_group);
            }
            
            OnReloaded();
        }
        
        private void AddStationGroup(Playlist group)
        {
            TreeIter iter = AppendValues(group.Title, String.Empty, null);
            
            foreach(Track track in group.Tracks) {
                AppendValues(iter, track.Title, track.Annotation, track);
            }
        }
        
        private void OnReloaded()
        {
            EventHandler handler = Reloaded;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }
        
        public string GetStation(TreeIter iter)
        {
            if(IterHasChild(iter)) {
                return GetValue(iter, 0) as string;
            }
            
            return null;
        }
        
        public Track GetTrack(TreeIter iter)
        {
            return GetValue(iter, 2) as Track;
        }
        
        public Track GetTrack(TreePath path)
        {
            TreeIter iter;
            
            if(GetIter(out iter, path)) {
                return GetTrack(iter);
            } 
            
            return null;
        }
        
        public RadioTrackInfo GetRadioTrackInfo(TreeIter iter)
        {
            return GetValue(iter, 3) as RadioTrackInfo;
        }
        
        public RadioTrackInfo GetRadioTrackInfo(TreePath path)
        {
            TreeIter iter;
            
            if(GetIter(out iter, path)) {
                return GetRadioTrackInfo(iter);
            } 
            
            return null;
        }
        
        public void SetRadioTrackInfo(TreeIter iter, RadioTrackInfo track)
        {
            SetValue(iter, 3, track);
        }
        
        public void SetRadioTrackInfo(TreePath path, RadioTrackInfo track)
        {
            TreeIter iter;
            
            if(GetIter(out iter, path)) {
                SetRadioTrackInfo(iter, track);
            }
        }
    }
}
