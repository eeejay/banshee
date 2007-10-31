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
using Banshee.Collection;

namespace Banshee.Metadata.MusicBrainz
{
    public class MusicBrainzQueryJob : MetadataServiceJob
    {
        private static string AmazonUriFormat = "http://images.amazon.com/images/P/{0}.01._SCLZZZZZZZ_.jpg";
    
        private TrackInfo track;
        private string asin;
        
        public MusicBrainzQueryJob(IBasicTrackInfo track, MetadataSettings settings)
        {
            Track = track;
            this.track = track as TrackInfo; 
            Settings = settings;
        }
        
        public MusicBrainzQueryJob(IBasicTrackInfo track, MetadataSettings settings, string asin) : this(track, settings)
        {
            this.asin = asin;
        }
        
        public override void Run()
        {
            Lookup();
        }
        
        public bool Lookup()
        {
            if(track == null || track.CoverArtFileName != null) {
                return false;
            }
            
            string album_artist_id = AlbumInfo.CreateArtistAlbumId(track.ArtistName, track.AlbumTitle, false);
            if(album_artist_id == null) {
                return false;
            } else if(File.Exists(Paths.GetCoverArtPath(album_artist_id))) {
                return false;
            } else if(!Settings.NetworkConnected) {
                return false;
            }
            
            if(asin == null) {
                asin = FindAsin();
                if(asin == null) {
                    return false;
                }
            }
            
            if(SaveHttpStreamPixbuf(new Uri(String.Format(AmazonUriFormat, asin)), album_artist_id, 
                new string [] { "image/gif" })) {
                StreamTag tag = new StreamTag();
                tag.Name = CommonTags.AlbumCoverId;
                tag.Value = album_artist_id;

                AddTag(tag);
                
                return true;
            }
            
            return false;
        }

        // MusicBrainz has this new XML API, so I'm using that here 
        // instead of using the stuff in the MusicBrainz namespace 
        // which sucks and doesn't even appear to work anymore.
        
        private string FindAsin()
        {
            Uri uri = new Uri(String.Format("http://musicbrainz.org/ws/1/release/?type=xml&artist={0}&title={1}",
                track.ArtistName, track.AlbumTitle));

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
