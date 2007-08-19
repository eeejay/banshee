//
// M3uPlaylistFormat.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;

using Mono.Unix;

using Banshee.Base;
using Banshee.Sources;
 
namespace Banshee.Playlists.Formats
{
    public class M3uPlaylistFormat : PlaylistFormatBase
    {
        public static readonly PlaylistFormatDescription FormatDescription = new PlaylistFormatDescription(
            typeof(M3uPlaylistFormat), Catalog.GetString("MPEG Version 3.0 Extended (*.m3u)"), "m3u");
        
        public M3uPlaylistFormat()
        {
        }
        
        public override void Load(Stream stream)
        {
            using(StreamReader reader = new StreamReader(stream)) {
                string line;
                int line_number = 0;
                bool extended = false;
                Dictionary<string, object> element = null;
                
                while((line = reader.ReadLine()) != null) {
                    line = line.Trim();
                    
                    if(line_number++ == 0 && line == "#EXTM3U") {
                        extended = true;
                    }
                    
                    if(line.Length == 0) {
                        continue;
                    }
                    
                    bool extinf = line.StartsWith("#EXTINF:");
                    if((extinf && !extended) || (!extinf && line[0] == '#')) {
                        continue;
                    }
                    
                    if(extinf) {
                        element = AddElement();
                        try {
                            ParseExtended(element, line);
                        } catch {
                        }
                        continue;
                    } else if(element == null) {
                        element = AddElement();
                    }
                    
                    element["uri"] = ResolveUri(line);
                    element = null;
                }
            }
        }
        
        private void ParseExtended(Dictionary<string, object> element, string line)
        {
            string split = line.Substring(8).TrimStart(',');
            string [] parts = split.Split(new char [] { ',' }, 2);
            
            if(parts.Length == 2) {
                try {
                    int seconds = Convert.ToInt32(parts[0]);
                    if(seconds > 0) {
                        element["duration"] = TimeSpan.FromSeconds(seconds);
                    }
                } catch {
                }
                
                element["title"] = parts[1].Trim();
            } else {
                element["title"] = split.Trim();
            }
        }
        
        public override void Save(Stream stream, Source source)
        {
            using(StreamWriter writer = new StreamWriter(stream)) {
                writer.WriteLine("#EXTM3U");
                foreach(TrackInfo track in source.Tracks) {
                    int duration = (int)Math.Round(track.Duration.TotalSeconds);
                    if(duration <= 0) {
                        duration = -1;
                    }
                    
                    writer.WriteLine("#EXTINF:{0},{1} - {2}", duration, track.DisplayArtist, track.DisplayTitle);
                    writer.WriteLine(ExportUri(track.Uri));
                }
            }
        }
    }
}
