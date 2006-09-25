/***************************************************************************
 *  HelixRemotePlayerEngine.cs
 *
 *  Copyright (C) 2006 Novell, Inc
 *  Written by Aaron Bockover <aaron@abock.org>
 *             Equalizer support by Ivan N. Zlatev <contact i-nZ.net>
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

using Helix;

using Banshee.Base;
using Banshee.MediaEngine;

namespace Banshee.MediaEngine.Helix
{    
    public class HelixRemotePlayerEngine : PlayerEngine, IEqualizer
    {
        private IRemotePlayer player;
        private uint timeout_id;
        private uint ping_id;
        
        public HelixRemotePlayerEngine()
        {
            player = RemotePlayer.Connect();
            player.Stop();
            player.Message += OnRemotePlayerMessage;
            
            ping_id = GLib.Timeout.Add(5000, delegate {
                if(player == null) {
                    return false;
                }
                
                player.Ping();
                return true;
            });
        }
        
        public override void Dispose()
        {
            base.Dispose();
            player.Shutdown();
            player = null;
            GLib.Source.Remove(ping_id);
        }
                
        protected override void OpenUri(SafeUri uri)
        {
            if(!player.OpenUri(uri.AbsoluteUri)) {
                throw new ApplicationException("Cannot open URI");
            }

            timeout_id = GLib.Timeout.Add(500, delegate {
                if(CurrentState == PlayerEngineState.Playing) {
                    OnEventChanged(PlayerEngineEvent.Iterate);
                }
                return true;
            });
        }
        
        public override void Close()
        {
            if(timeout_id > 0) {
                GLib.Source.Remove(timeout_id);
            }
            
            player.Stop();
            base.Close();
        }
        
        public override void Play()
        {
            player.Play();
        }
        
        public override void Pause()
        {
            player.Pause();
        }
        
        public void OnRemotePlayerMessage(MessageType messageType, IDictionary<string, object> args)
        {
            switch(messageType) {
                case MessageType.ContentConcluded:
                    Close();
                    OnEventChanged(PlayerEngineEvent.EndOfStream);
                    break;
                case MessageType.ContentState:
                    switch((ContentState)args["NewState"]) {
                        case ContentState.Paused:
                            OnStateChanged(PlayerEngineState.Paused);
                            break;
                        case ContentState.Playing:
                            OnStateChanged(PlayerEngineState.Playing);
                            break;
                        case ContentState.Loading:
                        case ContentState.Contacting:
                            break;
                        default:
                            OnStateChanged(PlayerEngineState.Idle);
                            break;
                    }
                    break;
                case MessageType.Buffering:
                    uint progress = (uint)args["Percent"];
                    OnEventChanged(PlayerEngineEvent.Buffering, 
                        null, (double)progress / 100.0);
                    break;
                case MessageType.Title:
                    string title = args["Title"] as string;
                    if(title == null || title.Trim() == String.Empty) {
                        break;
                    }
                
                    StreamTag tag = new StreamTag();
                    tag.Name = CommonTags.Title;
                    tag.Value = (string)args["Title"];
                    
                    OnTagFound(tag);
                    
                    break;
            }
        }
         
        public override ushort Volume {
            get { return (ushort)player.Volume; }
            set {
                player.Volume = (uint)value;
                OnEventChanged(PlayerEngineEvent.Volume);
            }
        }
    
        public override uint Position {
            get { return (uint)player.Position / 1000; }
            set {
                if(player.StartSeeking()) {
                    if(player.SetPosition(value * 1000)) {
                        OnEventChanged(PlayerEngineEvent.Seek);
                    }
                    
                    player.StopSeeking();
                }
            }
        }
        
        public override uint Length { 
            get { return (uint)player.Length / 1000; }
        }        
        
        public override bool CanSeek {
            get { return !player.IsLive; }
        }
        
        public override string Id {
            get { return "helix-remote"; }
        }
        
        public override string Name {
            get { return "Helix Remote"; }
        }
        
        private static string [] source_capabilities = { "file", "http" };
        public override IEnumerable SourceCapabilities {
            get { return source_capabilities; }
        }
        
        private static string [] decoder_capabilities = { "m4a", "mp3", "ram", "ra", "rm", "aac", "mp4" };
        public override IEnumerable ExplicitDecoderCapabilities {
            get { return decoder_capabilities; }
        }

        // IEqualizer implementation
        //
        // Helix range (out): -144..144
        // Plugin range (in): -100 .. 100
        // Helix = Plugin * 1.44 = Plugin * 144/100
        //
        
        private static uint [] eq_frequencies = new uint [] { 31, 62, 125, 250, 500, 1000, 2000, 4000, 8000, 16000 };
        private static Dictionary<uint, int> frequency_map = new Dictionary<uint, int>();
        
        public void SetEqualizerGain(uint frequency, int value)
        {
            if(value < -100 || value > 100) {
                throw new ArgumentOutOfRangeException("value must be in range -100..100");
            }
            
            if(!player.IsEqualizerEnabled) {
                player.IsEqualizerEnabled = true;
            }

            if(frequency_map.Count == 0) {
                for(int i = 0; i < EqualizerFrequencies.Length; i++) {
                    frequency_map.Add(EqualizerFrequencies[i], i);
                }
            }

            int frequency_id = -1;
            
            try {
                frequency_id = frequency_map[frequency];
            } catch {
                throw new ArgumentException("Invalid frequency");
            }
            
            player.SetEqualizerGain(frequency_id, value * 144 / 100);
        }

        public uint [] EqualizerFrequencies {
            get { return eq_frequencies; }
        }

        public int AmplifierLevel {
            get { return 0; }
            set { }
        }
    }
}
