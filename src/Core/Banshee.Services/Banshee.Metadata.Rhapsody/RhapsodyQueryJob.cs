//
// RhapsodyQueryJob.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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
using System.Net;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Metadata;
using Banshee.Kernel;
using Banshee.Streaming;

namespace Banshee.Metadata.Rhapsody
{
    public class RhapsodyQueryJob : MetadataServiceJob
    {
        private static Uri base_uri = new Uri("http://www.rhapsody.com/");
        
        public RhapsodyQueryJob(IBasicTrackInfo track, MetadataSettings settings)
        {
            Track = track;
            Settings = settings;
        }
        
        public override void Run()
        {
            if(Track == null) {
                return;
            }
        
            string album_artist_id = CoverArtSpec.CreateArtistAlbumId(Track.ArtistName, Track.AlbumTitle, false);
            
            if(album_artist_id == null || CoverArtSpec.CoverExists(album_artist_id) || !Settings.NetworkConnected) {
                return;
            }
            
            Uri data_uri = new Uri(base_uri, String.Format("/{0}/data.xml", album_artist_id.Replace('-', '/')));
        
            XmlDocument doc = new XmlDocument();
            Stream stream = GetHttpStream(data_uri);
            if(stream == null) {
                return;
            }

            using(stream) {
                doc.Load(stream);
            }

            XmlNode art_node = doc.DocumentElement.SelectSingleNode("/album/art/album-art[@size='large']/img");
            if(art_node != null && art_node.Attributes["src"] != null) {
                // awesome hack to get high resolution cover art from Rhapsody
                string second_attempt = art_node.Attributes["src"].Value;
                string first_attempt = second_attempt.Replace("170x170", "500x500");

                if(SaveHttpStreamCover(new Uri(first_attempt), album_artist_id, null) || 
                    SaveHttpStreamCover(new Uri(second_attempt), album_artist_id, null)) {
                    Log.Debug ("Downloaded cover art from Rhapsody", album_artist_id);
                    StreamTag tag = new StreamTag();
                    tag.Name = CommonTags.AlbumCoverId;
                    tag.Value = album_artist_id;
                
                    AddTag(tag);
                }
            }
        }
    }
}
