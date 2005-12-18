/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  VlcPlayerEngine.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Unix;

using Banshee.Base;
using Banshee.MediaEngine;

namespace Banshee.MediaEngine.Vlc
{    
    public class VlcPlayerEngine : IPlayerEngine
    {
        public event PlayerEngineErrorHandler Error;
        public event PlayerEngineVolumeChangedHandler VolumeChanged;
        public event PlayerEngineIterateHandler Iterate;
        public event EventHandler EndOfStream;
        
        private VLC player;
        private Thread iterateThread;
        
        private TrackInfo track;
        
        private uint timerId = 0;
        private bool loaded = false;
        private bool paused = false;
        private bool disabled;
        private bool shutdown;
        
        public bool Disabled {
            get { return disabled;  }
            set { disabled = value; }
        }
        
        public string ConfigName
        {
            get {
                return "vlc";
            }
        }
        
        public string EngineName
        {
            get {
                return "VLC";
            }
        }
        
        public string EngineLongName
        {
            get {
                return Catalog.GetString("VLC");
            }
        }
        
        public string EngineDetails
        {
            get {
                return Catalog.GetString(
                    "VLC is a highly portable multimedia player for various " +
                    "audio and video formats (MP3, AAC, OGG, FLAC, WMA, etc.) " +
                    "See http://videolan.org/vlc/");
            }
        }
        
        public int MajorVersion
        {
            get {
                return 0;
            }
        }
        
        public int MinorVersion
        {
            get {
                return 1;
            }
        }
        
        public string AuthorName
        {
            get {
                return "Aaron Bockover";
            }
        }
        
        public string AuthorEmail
        {
            get {
                return "aaron@aaronbock.net";
            }
        }
        
        public void Initialize()
        {
            player = new VLC();
        }
        
        public void TestInitialize()
        {
            player = new VLC();
            player.Dispose();
        }
        
        public void Dispose()
        {
            player.Dispose();
        }
                
        public bool Open(TrackInfo ti, Uri uri)
        {
            if(!ti.CanPlay) {
                loaded = false;
                EmitEndOfStream();
                return false;
            }
            
            track = ti;
            player.Open(uri.AbsoluteUri);

            timerId = GLib.Timeout.Add(500, delegate() {
                if(!player.IsPlaying && !paused) {
                    EmitEndOfStream();
                    return false;
                }
                
                PlayerEngineIterateArgs args = new PlayerEngineIterateArgs();
                args.Position = Position;
                EmitIterate(args);
                return true;
            });
            
            loaded = true;
            return true;
        }
        
        public void Close()
        {
            if(timerId > 0) {
                GLib.Source.Remove(timerId);
            }
            
            player.Stop();
            loaded = false;
        }
        
        public void Play()
        {
            player.Play();
            paused = false;
        }
        
        public void Pause()
        {
            player.Pause();
            paused = true;
        }

        public bool Loaded 
        {
            get {
                return loaded;
            }
        }
         
        public bool Playing
        {
            get {
                return player.IsPlaying;
            }
        }
         
        public ushort Volume
        {
            get {
                return (ushort)player.Volume;
            }
          
            set {
                player.Volume = (int)value;
                
                PlayerEngineVolumeChangedArgs args = 
					new PlayerEngineVolumeChangedArgs();
				args.Volume = Volume;
				EmitVolumeChanged(args);
            }
        }
    
        public uint Position
        {
            get {
                return (uint)player.Time;
            }
          
            set {
                player.Time = (int)value;
            }
        }
    
        public uint Length 
        { 
            get {
                return (uint)player.Length;
            }
        }
        
        public TrackInfo Track
        {
            get {
                return track;
            }
        }
        
        protected void EmitError(PlayerEngineErrorArgs args)
        {
            PlayerEngineErrorHandler handler = Error;
            if(handler != null)
                handler(this, args);
        }
            
        protected void EmitVolumeChanged(PlayerEngineVolumeChangedArgs args)
        {
            PlayerEngineVolumeChangedHandler handler = VolumeChanged;
            if(handler != null)
                handler(this, args);
        } 
        
        protected void EmitIterate(PlayerEngineIterateArgs args)
        {
            PlayerEngineIterateHandler handler = Iterate;
            if(handler != null)
                handler(this, args);
        }
        
        protected void EmitEndOfStream()
        {
            EventHandler handler = EndOfStream;
            if(handler != null)
                handler(this, new EventArgs());
        }
    }
}
