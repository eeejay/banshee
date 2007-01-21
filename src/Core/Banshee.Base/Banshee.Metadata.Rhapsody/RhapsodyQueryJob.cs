/***************************************************************************
 *  RhapsodyQueryJob.cs
 *
 *  Copyright (C) 2006-2007 Novell, Inc.
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
using System.IO;
using System.Net;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Gdk;

using Banshee.Base;
using Banshee.Metadata;
using Banshee.Kernel;

namespace Banshee.Metadata.Rhapsody
{
    public class RhapsodyQueryJob : MetadataServiceJob
    {
        private static Uri base_uri = new Uri("http://www.rhapsody.com/");
        
        public RhapsodyQueryJob(IBasicTrackInfo track)
        {
            Track = track;
        }
        
        public override void Run()
        {
            if(Track == null || (Track is TrackInfo && (Track as TrackInfo).CoverArtFileName != null)) {
                return;
            }
        
            string album_artist_id = TrackInfo.CreateArtistAlbumID(Track.Artist, Track.Album, false);
            if(album_artist_id == null) {
                return;
            } else if(File.Exists(Paths.GetCoverArtPath(album_artist_id))) {
                return;
            } else if(!Globals.Network.Connected) {
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
                Uri art_uri = new Uri(art_node.Attributes["src"].Value);
                if(SaveHttpStreamPixbuf(art_uri, album_artist_id, null)) {
                    StreamTag tag = new StreamTag();
                    tag.Name = CommonTags.AlbumCoverID;
                    tag.Value = album_artist_id;
                
                    AddTag(tag);
                }
            }
        }
    }
}
