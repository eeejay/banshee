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
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

using Helix;

using Banshee.Base;
using Banshee.MediaEngine;

public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.MediaEngine.HelixRemote.HelixRemotePlayerEngine)
        };
    }
}

namespace Banshee.MediaEngine.HelixRemote
{
    public class HelixClipParser
    {
        private string artist;
        private string title;
        private string album;
        private TimeSpan duration;
        private SafeUri more_info;
        
        public HelixClipParser(string clip)
        {
            Parse(clip);
        }
        
        private void Parse(string clip)
        {
            string clip_s = clip.Trim();
            
            if(!clip_s.StartsWith("clipinfo:")) {
                title = clip_s;
                return;
            }
            
            foreach(string part in clip_s.Substring(9).Split('|')) {
                string [] kvp = part.Split(new char [] {'='}, 2);
                if(kvp.Length != 2) {
                    continue;
                }

                string key = kvp[0].ToLower().Trim();
                string value = kvp[1].Trim();
                
                try {
                    switch(key) {
                        case "title": title = value; break;
                        case "artist name": artist = value; break;
                        case "album name": album = value; break;
                        case "duration": duration = TimeSpan.FromSeconds(Int32.Parse(value)); break;
                        case "track:rhapsody track id": break;
                        default: break;
                    }
                } catch {
                }
            }
            
            SafeUri album_uri = null;
            if(artist != null && album != null) {
                album_uri = new SafeUri(String.Format("http://www.rhapsody.com/{0}/{1}",
                    MakeUriPart(artist), MakeUriPart(album), MakeUriPart(title)));
            }
            
            if(artist != null && album != null && title != null) {
                more_info = new SafeUri(String.Format("http://www.rhapsody.com/{0}/{1}/{2}", 
                    MakeUriPart(artist), MakeUriPart(album), MakeUriPart(title)));
            } else if(artist != null && album != null) {
                more_info = album_uri;
            } else if(artist != null) {
                more_info = new SafeUri(String.Format("http://www.rhapsody.com/{0}", MakeUriPart(artist)));
            }
        }
        
        private string MakeUriPart(string str)
        {
            return Regex.Replace(str, @"[^A-Za-z0-9]*", "");
        }
        
        public string Artist {
            get { return artist; }
        }
        
        public string Title {
            get { return title; }
        }
        
        public string Album {
            get { return album; }
        }
        
        public SafeUri MoreInfo {
            get { return more_info; }
        }
        
        public TimeSpan Duration {
            get { return duration; }
        }
    }

    public class HelixRemotePlayerEngine : PlayerEngine, IEqualizer
    {
        private IRemotePlayer player;
        private uint timeout_id;
        private uint ping_id;
        private uint position_mark = 0;
        private uint stream_songs = 0;
        
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
            
            stream_songs = 0;
            position_mark = 0;

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
                            break;
                        case ContentState.Contacting:
                            OnStateChanged(PlayerEngineState.Contacting);
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
                    
                    if(CurrentTrack == null) {
                        break;
                    }
                    
                    HelixClipParser parser = new HelixClipParser(title);
                    
                    if(parser.Title != null) {
                        StreamTag tag = new StreamTag();
                        tag.Name = CommonTags.Title;
                        tag.Value = parser.Title;
                        OnTagFound(tag);
                    }
                     
                    if(parser.Artist != null) {
                        StreamTag tag = new StreamTag();
                        tag.Name = CommonTags.Artist;
                        tag.Value = parser.Artist;
                        OnTagFound(tag);
                    }
                    
                    if(parser.Album != null) {
                        StreamTag tag = new StreamTag();
                        tag.Name = CommonTags.Album;
                        tag.Value = parser.Album;
                        OnTagFound(tag);
                    }
                    
                    if(!parser.Duration.Equals(TimeSpan.Zero)) {
                        StreamTag tag = new StreamTag();
                        tag.Name = CommonTags.Duration;
                        tag.Value = parser.Duration;
                        OnTagFound(tag);
                        position_mark = player.Position;
                        stream_songs++;
                    }
                    
                    if(parser.MoreInfo != null) {
                        StreamTag tag = new StreamTag();
                        tag.Name = CommonTags.MoreInfoUri;
                        tag.Value = parser.MoreInfo;
                        OnTagFound(tag);
                        
                        if(CurrentTrack != null) {
                            CurrentTrack.CoverArtFileName = null;
                            Banshee.Metadata.MultipleMetadataProvider.Instance.Lookup(CurrentTrack);
                        }
                    }
                    
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
            get { return (uint)(player.Position - (stream_songs > 1 ? position_mark : 0)) / 1000; }
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
