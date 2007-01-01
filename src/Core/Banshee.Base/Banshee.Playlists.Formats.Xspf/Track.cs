/***************************************************************************
 *  Track.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
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
using System.Xml;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Banshee.Playlists.Formats.Xspf
{
    public class Track : XspfBaseObject
    {
        // TODO: Add attribution, extension support
        
        private Uri base_uri;
    
        private string album;
        
        private uint track_num;
        private TimeSpan duration;
    
        private List<Uri> locations;
        private List<Uri> identifiers;
        
        public Track()
        {
        }
        
        internal void Load(Playlist playlist, XmlNode node, XmlNamespaceManager xmlns)
        {
            XmlAttribute base_attr = node.Attributes["xml:base"];
            if(base_attr != null) {
                if(playlist.ResolvedBaseUri != null) {
                    base_uri = new Uri(playlist.ResolvedBaseUri, base_attr.Value);
                } else {
                    base_uri = new Uri(base_attr.Value);
                }
            } else {
                base_uri = playlist.ResolvedBaseUri;
            }
            
            LoadBase(node, xmlns);
            
            album = XmlUtil.ReadString(node, xmlns, "xspf:album");

            track_num = XmlUtil.ReadUInt(node, xmlns, "xspf:trackNum");
            duration = TimeSpan.FromMilliseconds(XmlUtil.ReadUInt(node, xmlns, "xspf:duration"));
            
            locations = XmlUtil.ReadUris(node, xmlns, ResolvedBaseUri, "xspf:location");
            identifiers = XmlUtil.ReadUris(node, xmlns, ResolvedBaseUri, "xspf:identifier");
        }
        
        public override Uri ResolvedBaseUri {
            get { return base_uri; }
        }
        
        public string Album {
            get { return album; }
            set { album = value; }
        }
        
        public uint TrackNumber {
            get { return track_num; }
            set { track_num = value; }
        }
        
        public TimeSpan Duration {
            get { return duration; }
            set { duration = value; }
        }
                
        public ReadOnlyCollection<Uri> Locations {
            get { return new ReadOnlyCollection<Uri>(locations); }
        }
        
        public ReadOnlyCollection<Uri> Identifiers {
            get { return new ReadOnlyCollection<Uri>(identifiers); }
        }
    }
}
