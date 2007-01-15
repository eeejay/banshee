/***************************************************************************
 *  RadioTrackInfo.cs
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

using Banshee.Base;
using Banshee.Playlists.Formats.Xspf;
 
namespace Banshee.Plugins.Radio
{   
    public class RadioTrackInfo : TrackInfo
    {
        private Track track;
        private List<SafeUri> stream_uris = new List<SafeUri>();
        private int stream_index = 0;
        private bool loaded = false;
        private bool parsing_playlist = false;
        
        public event EventHandler ParsingPlaylistEvent;
        
        public RadioTrackInfo(Track track) : base()
        {
            Title = track.Title;
            Artist = track.Creator;
            this.track = track;
            is_live = true;
        }
        
        public void Play()
        {
            if(!loaded) {
                OnParsingPlaylistStarted();
                ThreadAssist.Spawn(delegate {
                    LoadStreamUris();
                });
                return;
            }
            
            Title = track.Title;
            Artist = track.Creator;
            Album = null;
            Duration = TimeSpan.Zero;
            CoverArtFileName = null;
            
            lock(((ICollection)stream_uris).SyncRoot) {
                if(stream_uris.Count > 0) {
                    Uri = stream_uris[stream_index];
                    PlayerEngineCore.OpenPlay(this);
                }
            }
        }
        
        public void PlayNextStream()
        {
            if(stream_index < stream_uris.Count - 1) {
                stream_index++;
                Play();
            }
        }
        
        private void LoadStreamUris()
        {
            lock(((ICollection)stream_uris).SyncRoot) {
                if(!Gnome.Vfs.Vfs.Initialized) {
                    Gnome.Vfs.Vfs.Initialize();
                }
                
                TotemPlParser.Parser parser = new TotemPlParser.Parser();
                parser.Entry += OnHavePlaylistEntry;
                
                foreach(Uri uri in track.Locations) {
                    try {
                        if(parser.Parse(uri.AbsoluteUri, false) == TotemPlParser.Result.Unhandled) {
                            stream_uris.Add(new SafeUri(uri.AbsoluteUri));
                        }
                    } catch(Exception e) {
                        Console.WriteLine(e);
                    }
                }
                
                parser.Entry -= OnHavePlaylistEntry;
                parser = null;
                
                loaded = true;
            }
            
            ThreadAssist.ProxyToMain(delegate {
                OnParsingPlaylistFinished();
                Play();
            });
        }
        
        private void OnParsingPlaylistStarted()
        {
            parsing_playlist = true;
            OnParsingPlaylistEvent();
        }
        
        private void OnParsingPlaylistFinished()
        {
            parsing_playlist = false;
            OnParsingPlaylistEvent();
        }
        
        private void OnParsingPlaylistEvent()
        {
            EventHandler handler = ParsingPlaylistEvent;
            if(handler != null) {
                handler(this, EventArgs.Empty);
            }
        }
        
        private void OnHavePlaylistEntry(object o, TotemPlParser.EntryArgs args)
        {
            stream_uris.Add(new SafeUri(args.Uri));
        }
        
        public Track XspfTrack {
            get { return track; }
        }
        
        public bool ParsingPlaylist {
            get { return parsing_playlist; }
        }
    }
}
