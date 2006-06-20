/***************************************************************************
 *  BurnerSource.cs
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
using System.Collections;
using System.Collections.Generic;
using Mono.Unix;

using Hal;
using Gtk;

using Banshee.Base;
using Banshee.Sources;
using Banshee.Widgets;
using Banshee.Cdrom;

namespace Banshee.Burner
{
    public class BurnerSource : Source
    {
        private List<TrackInfo> tracks = new List<TrackInfo>();
        
        private Alignment container;
        private Box box;
        private BurnerConfigurationPane configuration;
        private BurnerSession session = new BurnerSession();
        private BurnerOptionsDialog burner_options = null;
    
        private bool has_default_name = true;
    
        public BurnerSource(IRecorder recorder) : this()
        {
            session.Recorder = recorder;
        }
    
        public BurnerSource() : base(Catalog.GetString("New CD"), 600)
        {
            Initialize();
        }
        
        private void Initialize()
        {
            SourceManager.AddSource(this);
            
            container = new Alignment(0.0f, 0.0f, 1.0f, 1.0f);
            
            configuration = new BurnerConfigurationPane(this);
            configuration.Show();
            
            box = new VBox();
            box.Spacing = 8;
            
            box.PackStart(container, true, true, 0);
            box.PackStart(configuration, false, false, 0);
            box.PackStart(new HSeparator(), false, false, 0);
            
            container.Show();
            box.ShowAll();
        }
        
        protected override void OnDispose()
        {
            if(burner_options != null) {
                burner_options.Dialog.Respond(ResponseType.Close);
            }
        }
        
        protected override bool UpdateName(string oldName, string newName)
        {
            if(oldName.Equals(newName)) {
                return false;
            }
            
            Name = newName;
            session.DiscName = newName;
            has_default_name = false;
            
            return true;
        }
        
        public void Burn()
        {
            burner_options = new BurnerOptionsDialog(this);
            burner_options.Dialog.Response += OnBurnerOptionsResponse;
            burner_options.Run();
        }
        
        private void OnBurnerOptionsResponse(object o, ResponseArgs args)
        {
            if(args.ResponseId == ResponseType.Ok) {
                session.ClearTracks();
                
                foreach(TrackInfo track in tracks) {
                    session.AddTrack(track, "/");
                }
                
                if(!session.Record()) {
                    return;
                }
            }
            
            if(burner_options != null) {
                burner_options.Destroy();
                burner_options = null;
            }
        }
        
        public override void Activate()
        {
            InterfaceElements.DetachPlaylistContainer();
            container.Add(InterfaceElements.PlaylistContainer);
        }
        
        public override bool Unmap()
        {
            Dispose();
            SourceManager.RemoveSource(this);
            return true;
        }
        
        public override void AddTrack(TrackInfo track)
        {
            lock(TracksMutex) {
                tracks.Add(track);
            }
            
            OnTrackAdded(track);
        }
        
        public override void RemoveTrack(TrackInfo track)
        {
            lock(TracksMutex) {
                tracks.Remove(track);
            }
            
            OnTrackRemoved(track);    
        }
        
        public override void Reorder(TrackInfo track, int position)
        {
            lock(TracksMutex) {
                tracks.Remove(track);
                tracks.Insert(position, track);
            }
        }
        
        public override void SourceDrop(Source source)
        {
            if(!(source is PlaylistSource)) {
                return;
            }
            
            if(source.Count > 0 && tracks.Count == 0 && has_default_name) {
                Rename(source.Name);
            }
                
            foreach(TrackInfo track in source.Tracks) {
                AddTrack(track);
            }
        }
            
        public override IEnumerable Tracks {
            get { return tracks; }
        }
        
        public override object TracksMutex {
            get { return (tracks as ICollection).SyncRoot; }
        }
        
        public override int Count {
            get { return tracks.Count; }
        }  
        
        private static Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(22, "drive-cdrom");
        
        public override Gdk.Pixbuf Icon {
            get { return icon; }
        }
        
        public override Gtk.Widget ViewWidget {
            get { return box; }
        }
        
        public override bool AcceptsInput {
            get { return true; }
        }
        
        public override bool SearchEnabled {
            get { return false; }
        }
        
        public override string GenericName {
            get { return Catalog.GetString("CD Session"); }
        }
        
        internal BurnerSession Session {
            get { return session; }
        }
    }
}
