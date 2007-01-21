/***************************************************************************
 *  MusicBrainzQueryJob.cs
 *
 *  Copyright (C) 2006-2007 Novell, Inc.
 *  Written by James Willcox <snorp@novell.com>
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
using System.Collections.Generic;

using Banshee.Base;
using Banshee.Metadata;
using Banshee.Kernel;

namespace Banshee.Metadata.MusicBrainz
{
    public class MusicBrainzQueryJob : MetadataServiceJob
    {
        private static string AmazonUriFormat = "http://images.amazon.com/images/P/{0}.01._SCLZZZZZZZ_.jpg";
    
        private TrackInfo track;
        
        public MusicBrainzQueryJob(IBasicTrackInfo track)
        {
            Track = track;
            this.track = track as TrackInfo; 
        }
        
        public override void Run()
        {
            if(track == null || track.CoverArtFileName != null) {
                return;
            }
            
            string album_artist_id = TrackInfo.CreateArtistAlbumID(track.Artist, track.Album, false);
            if(album_artist_id == null) {
                return;
            } else if(File.Exists(Paths.GetCoverArtPath(album_artist_id))) {
                return;
            } else if(!Globals.Network.Connected) {
                return;
            }
            
            string asin = FindAsin();
            if (asin == null) {
                return;
            }
            
            SaveHttpStream(new Uri(String.Format(AmazonUriFormat, asin)),  Paths.GetCoverArtPath(album_artist_id));
            
            StreamTag tag = new StreamTag();
            tag.Name = CommonTags.AlbumCoverID;
            tag.Value = album_artist_id;
            
            AddTag(tag);
        }

        // MusicBrainz has this new XML API, so I'm using that here 
        // instead of using the stuff in the MusicBrainz namespace 
        // which sucks and doesn't even appear to work anymore.
        
        private string FindAsin()
        {
            Uri uri = new Uri(String.Format("http://musicbrainz.org/ws/1/release/?type=xml&artist={0}&title={1}",
                track.Artist, track.Album));

            XmlTextReader reader = new XmlTextReader(GetHttpStream(uri));

            bool haveMatch = false;
            
            while(reader.Read()) {
                if(reader.NodeType == XmlNodeType.Element) {
                    switch (reader.LocalName) {
                        case "release":
                            haveMatch = reader["ext:score"] == "100";
                            break;
                        case "asin":
                            if(haveMatch) {
                                return reader.ReadString();
                            }
                        break;
                    default:
                        break;
                    }
                }
            }

            return null;
        }
    }
}
