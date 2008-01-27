/***************************************************************************
 *  AbstractPlaylistSource.cs
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
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
using System.Data;
using System.Collections;
using System.Collections.Generic;
using Mono.Unix;

using Banshee.Base;
using Banshee.Database;
using Banshee.Collection;
using Banshee.Configuration;

namespace Banshee.Sources
{
    public abstract class AbstractPlaylistSource : ChildSource
    {
        protected List<TrackInfo> tracks = new List<TrackInfo>();
        protected int id;
        
        public virtual int Id {
            get { return id; }
            protected set {
                id = value;
            }
        }

        private static string unmap_label = Catalog.GetString("Delete Playlist");
        public override string UnmapLabel {
            get { return unmap_label; }
        }

        private static string generic_name = Catalog.GetString("Playlist");
        public override string GenericName {
            get { return generic_name; }
        }

        public AbstractPlaylistSource(string name, int id) : base(name, id) {}

        public AbstractPlaylistSource(string name) : base(name, 100) {}

        protected override bool UpdateName(string oldName, string newName)
        {
            if (newName.Length > 256) {
                newName = newName.Substring (0, 256);
            }
            
            if(oldName.Equals(newName)) {
                return false;
            }
          
            Name = newName;
            return true;
        }

        public override void AddTrack(TrackInfo track)
        {
            lock(TracksMutex) {
                tracks.Add(track);
            }
            OnUpdated();
            OnTrackAdded(track);
        }

        public void ClearTracks()
        {
            tracks.Clear();
        }
        
        public override void RemoveTrack(TrackInfo track)
        {
            lock(TracksMutex) {
                tracks.Remove(track);
            }
        }
        
        public bool ContainsTrack(TrackInfo track)
        {
            return tracks.Contains(track);
        }
        
        public override bool Unmap()
        {
            if(Count > 0 && !ConfirmUnmap(this)) {
                return false;
            }
        
            tracks.Clear();
            
            SourceManager.RemoveSource(this);

            return true;
        }
        
        public override IEnumerable<TrackInfo> Tracks {
            get { return tracks; }
        }
        
        public override object TracksMutex {
            get { return ((IList)tracks).SyncRoot; }
        }
        
        public override int Count {
            get { return tracks.Count; }
        }  
        
        public override bool IsDragSource {
            get { return true; }
        }        
        
        private static Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(22, "source-playlist-22");
        public override Gdk.Pixbuf Icon {
            get { return icon; }
        }
    
        public static bool ConfirmUnmap(Source source)
        {
            string key = "no_confirm_unmap_" + source.GetType().Name.ToLower();
            bool do_not_ask = ConfigurationClient.Get<bool>("sources", key, false);
            
            if(do_not_ask) {
                return true;
            }
        
            Banshee.Widgets.HigMessageDialog dialog = new Banshee.Widgets.HigMessageDialog(
                InterfaceElements.MainWindow,
                Gtk.DialogFlags.Modal,
                Gtk.MessageType.Question,
                Gtk.ButtonsType.Cancel,
                String.Format(Catalog.GetString("Are you sure you want to delete this {0}?"),
                    source.GenericName.ToLower()),
                source.Name);
            
            dialog.AddButton(Gtk.Stock.Delete, Gtk.ResponseType.Ok, false);
            
            Gtk.Alignment alignment = new Gtk.Alignment(0.0f, 0.0f, 0.0f, 0.0f);
            alignment.TopPadding = 10;
            Gtk.CheckButton confirm_button = new Gtk.CheckButton(String.Format(Catalog.GetString(
                "Do not ask me this again"), source.GenericName.ToLower()));
            confirm_button.Toggled += delegate {
                do_not_ask = confirm_button.Active;
            };
            alignment.Add(confirm_button);
            alignment.ShowAll();
            dialog.LabelVBox.PackStart(alignment, false, false, 0);
            
            try {
                if(dialog.Run() == (int)Gtk.ResponseType.Ok) {
                    ConfigurationClient.Set<bool>("sources", key, do_not_ask);
                    return true;
                }
                
                return false;
            } finally {
                dialog.Destroy();
            }
        }
    }
}
