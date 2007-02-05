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
using System.Collections.Generic;
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
            typeof(string), 
            typeof(string), 
            typeof(Track),
            typeof(RadioTrackInfo), 
            typeof(StationGroup))
        {
            this.plugin = plugin;
            
            plugin.StationManager.StationsLoaded += OnStationsLoaded;
            plugin.StationManager.StationGroupAdded += OnStationGroupAdded;
            plugin.StationManager.StationGroupRemoved += OnStationGroupRemoved;
            plugin.StationManager.StationAdded += OnStationAdded;
            plugin.StationManager.StationRemoved += OnStationRemoved;
        }
        
        private void OnStationsLoaded(object o, EventArgs args)
        {
            Clear();
            
            foreach(StationGroup station_group in plugin.StationManager.StationGroups) {
                AddStationGroup(station_group);
            }
            
            OnReloaded();
        }
        
        private void OnStationAdded(object o, StationManager.StationArgs args)
        {
            AddStation(args.Group, args.Station);
        }
        
        private void OnStationRemoved(object o, StationManager.StationArgs args)
        {
            RemoveStation(args.Group, args.Station);
        }
        
        private void OnStationGroupAdded(object o, StationManager.StationGroupArgs args)
        {
            AddStationGroup(args.Group);
        }
        
        private void OnStationGroupRemoved(object o, StationManager.StationGroupArgs args)
        {
            RemoveStationGroup(args.Group);
        }
        
        private void AddStation(StationGroup group, Track track)
        {
            TreeIter iter;
            if(FindStationGroup(group, out iter) || FindStationGroup(group.Title, out iter)) {
                TreeIter new_iter = AppendNode(iter);
                UpdateStation(new_iter, track);
                SetValue(new_iter, 4, group);
            }
        }
        
        public void UpdateStation(TreeIter iter, Track track)
        {
            SetValue(iter, 0, track.Title);
            SetValue(iter, 1, track.Annotation);
            SetValue(iter, 2, track);
        }
        
        private void RemoveStation(StationGroup group, Track track)
        {
            TreeIter iter;
            TreeIter parent;
            
            if(FindStation(group, track, out iter)) {
                bool has_parent = IterParent(out parent, iter);
                Remove(ref iter);
                
                if(has_parent && !parent.Equals(TreeIter.Zero) && IterNChildren(parent) == 0) {
                    Remove(ref parent);
                }
            }
        }
        
        private void AddStationGroup(StationGroup group)
        {
            TreeIter iter;
            
            if(!FindStationGroup(group.Title, out iter)) {
                iter = AppendValues(group.Title, String.Empty, null, null, null);
            }
            
            foreach(Track track in group.Tracks) {
                AppendValues(iter, track.Title, track.Annotation, track, null, group);
            }
        }
        
        private void RemoveStationGroup(StationGroup group)
        {
            TreeIter iter;
            if(FindStationGroup(group, out iter)) {
                Remove(ref iter);
            }
        }
        
        private bool FindStation(StationGroup group, Track station, out TreeIter out_iter)
        {
            TreeIter parent;
            
            if(!FindStationGroup(group, out parent) && !FindStationGroup(group.Title, out parent)) {
                out_iter = TreeIter.Zero;
                return false;
            }
            
            for(int i = 0, n = IterNChildren(parent); i < n; i++) {
                TreeIter iter;
                if(IterNthChild(out iter, parent, i)) {
                    Track compare_station = GetTrack(iter);
                    if(compare_station == station) {
                        out_iter = iter;
                        return true;
                    }
                }
            }
            
            out_iter = TreeIter.Zero;
            return false;
        }
        
        private bool FindStationGroup(StationGroup group, out TreeIter out_iter)
        {
            for(int i = 0, n = IterNChildren(); i < n; i++) {
                TreeIter iter;
                if(IterNthChild(out iter, i)) {
                    StationGroup compare_group = GetStationGroup(iter);
                    if(compare_group == group) {
                        out_iter = iter;
                        return true;
                    }
                }
            }
            
            out_iter = TreeIter.Zero;
            return false;
        }
        
        private bool FindStationGroup(string group_title, out TreeIter out_iter)
        {
            for(int i = 0, n = IterNChildren(); i < n; i++) {
                TreeIter iter;
                if(IterNthChild(out iter, i)) {
                    if(String.Compare((string)GetValue(iter, 0), group_title) == 0) {
                        out_iter = iter;
                        return true;
                    }
                }
            }
            
            out_iter = TreeIter.Zero;
            return false;
        }
        
        private void OnReloaded()
        {
            EventHandler handler = Reloaded;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }
        
        public void UpdateStationGroup(StationGroup group)
        {
            TreeIter iter;
            if(FindStationGroup(group, out iter)) {
                SetValue(iter, 0, group.Title);
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
        
        public StationGroup GetStationGroup(TreeIter iter)
        {
            return GetValue(iter, 4) as StationGroup;
        }
        
        public StationGroup GetStationGroup(TreePath path)
        {
            TreeIter iter;
            
            if(GetIter(out iter, path)) {
                return GetStationGroup(iter);
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
        
        public IEnumerable<string> StationGroupNames {
            get {
                for(int i = 0, n = IterNChildren(); i < n; i++) {
                    TreeIter iter;
                    if(IterNthChild(out iter, i)) {
                        yield return (string)GetValue(iter, 0);
                    }
                }
            }
        }
    }
}
