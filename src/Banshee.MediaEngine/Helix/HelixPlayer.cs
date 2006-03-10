/***************************************************************************
 *  HelixPlayer.cs
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
using System.Threading;
using Mono.Unix;

using Helix;
using Banshee.Base;
using Banshee.MediaEngine;

namespace Banshee.MediaEngine.Helix
{
    public class HelixPlayerEngine : PlayerEngine
    {
        private HxPlayer player;
        private DateTime last_iterate_emit;
        private Thread iterate_thread;
        
        private bool shutdown;
        
        public HelixPlayerEngine()
        {
            player = new HxPlayer();
            player.Muted = false;
            player.ContentConcluded += OnContentConcluded;
            player.ErrorOccurred += OnErrorOccurred;
            player.ContentStateChanged += OnContentStateChanged;
            
            iterate_thread = new Thread(new ThreadStart(DoIterate));
            iterate_thread.IsBackground = true;
            iterate_thread.Start();
        }
        
        public override void Dispose()
        {
            base.Dispose();
            shutdown = true;
        }
        
        protected override void OpenUri(Uri uri)
        {
            player.OpenUri(uri.AbsoluteUri);
        }
        
        public override void Close()
        {
            player.Stop();
            base.Close();
        }
        
        public override void Play()
        {
            if(player.State == HxContentState.Stopped || player.State == HxContentState.Paused) {
                player.Play();
                OnStateChanged(PlayerEngineState.Playing);
            }
        }
        
        public override void Pause()
        {
            if(player.State == HxContentState.Playing) {
                player.Pause();
                OnStateChanged(PlayerEngineState.Paused);
            }
        }
        
        private void DoIterate()
        {
            while(!shutdown) {
                player.Iterate();
            
                if(DateTime.Now - last_iterate_emit >= new TimeSpan(0, 0, 0, 0, 500) 
                    && player.State == HxContentState.Playing) {
                    OnEventChanged(PlayerEngineEvent.Iterate);
                    last_iterate_emit = DateTime.Now;
                }

                Thread.Sleep(HxPlayer.PumpEventDelay);
            }
        }
        
        private void OnContentConcluded(object o, HxPlayerArgs args)
        {
            OnEventChanged(PlayerEngineEvent.EndOfStream);
        }
        
        private void OnContentStateChanged(object o, ContentStateChangedArgs args)
        {
            //Console.WriteLine("Helix Content State Changed: {0} -> {1}", args.NewState, args.OldState);
        }
        
        private void OnErrorOccurred(object o, ErrorOccurredArgs args)
        {
            OnEventChanged(PlayerEngineEvent.Error, args.Error + ": " + args.UserError);
        }
        
        public override uint Position {
            get { return player.Position / 1000; }
            set { 
                player.Position = value * 1000;
                OnEventChanged(PlayerEngineEvent.Seek);
            }
        }
        
        public override uint Length {
            get { return player.Length / 1000; }
        }
        
        public override ushort Volume {
            get { return player.Volume; }
            set {
                player.Volume = value;
                OnEventChanged(PlayerEngineEvent.Volume);
            }
        } 
        
        public override string Id {
            get { return "helix"; }
        }
        
        public override string Name {
            get { return "Helix"; }
        }
        
        private static string [] source_capabilities = { "file" };
        public override string [] SourceCapabilities {
            get { return source_capabilities; }
        }
    }
}
