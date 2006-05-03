/***************************************************************************
 *  VlcPlayerEngine.cs
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
using System.Runtime.InteropServices;
using System.Threading;
using Mono.Unix;

using Banshee.Base;
using Banshee.MediaEngine;

namespace Banshee.MediaEngine.Vlc
{    
    public class VlcPlayerEngine : PlayerEngine
    {
        private VLC player;     
        private uint timerId = 0;
        
        public VlcPlayerEngine()
        {
            player = new VLC();
        }
        
        public override void Dispose()
        {
            base.Dispose();
            player.Dispose();
        }
                
        protected override void OpenUri(SafeUri uri)
        {
            player.Open(uri.AbsoluteUri);

            timerId = GLib.Timeout.Add(500, delegate {
                if(!player.IsPlaying && CurrentState != PlayerEngineState.Paused) {
                    OnEventChanged(PlayerEngineEvent.EndOfStream);
                    return false;
                }
                
                OnEventChanged(PlayerEngineEvent.Iterate);
                return true;
            });
        }
        
        public override void Close()
        {
            if(timerId > 0) {
                GLib.Source.Remove(timerId);
            }
            
            player.Stop();
            base.Close();
        }
        
        public override void Play()
        {
            player.Play();
            OnStateChanged(PlayerEngineState.Playing);
        }
        
        public override void Pause()
        {
            player.Pause();
            OnStateChanged(PlayerEngineState.Paused);
        }
         
        public override ushort Volume {
            get { return (ushort)player.Volume; }
            set {
                player.Volume = (int)value;
                OnEventChanged(PlayerEngineEvent.Volume);
            }
        }
    
        public override uint Position {
            get { return (uint)player.Time; }
            set {
                player.Time = (int)value;
                OnEventChanged(PlayerEngineEvent.Seek);
            }
        }
        
        public override uint Length { 
            get { return (uint)player.Length; }
        }        
        
        public override string Id {
            get { return "vlc"; }
        }
        
        public override string Name {
            get { return "VLC"; }
        }
        
        private static string [] source_capabilities = { "file", "http" };
        public override System.Collections.IEnumerable SourceCapabilities {
            get { return source_capabilities; }
        }

		public override System.Collections.IEnumerable ExplicitDecoderCapabilities {
			get { return null; }
		}
    }
}
