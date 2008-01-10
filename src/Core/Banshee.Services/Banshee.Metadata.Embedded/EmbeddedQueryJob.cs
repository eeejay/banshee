//
// EmbeddedQueryJob.cs
//
// Authors:
//   Trey Ethridge <tale@juno.com>
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
using System.Collections.Generic;

using TagLib;

using Banshee.Base;
using Banshee.Metadata;
using Banshee.Collection;
using Banshee.Streaming;

namespace Banshee.Metadata.Embedded
{
    public class EmbeddedQueryJob : MetadataServiceJob
    {
        private TrackInfo track;
        
        public EmbeddedQueryJob(IBasicTrackInfo track, MetadataSettings settings)
        {
            Track = track;
            this.track = track as TrackInfo;
            Settings = settings;
        }
        
        public override void Run()
        {
            if(track == null || CoverArtSpec.CoverExists (track.ArtistAlbumId)) {
                return;
            }
          
            Fetch();
        }
        
        protected void Fetch()
        {
            string artist_album_id = track.ArtistAlbumId;

            if(artist_album_id == null) {
                return;
            }
            
            IPicture [] pictures = GetEmbeddedPictures(track.Uri);
                    
            if(pictures != null && pictures.Length > 0) {
                int preferred_index = GetPictureIndexToUse(pictures);
                IPicture picture = pictures[preferred_index];
                string path = CoverArtSpec.GetPath(artist_album_id);
                
                if(SavePicture(picture, path)) {    
                    StreamTag tag = new StreamTag();
                    tag.Name = CommonTags.AlbumCoverId;
                    tag.Value = artist_album_id;   
                    
                    AddTag(tag);
                } 
            }
        }

        protected IPicture [] GetEmbeddedPictures(SafeUri uri)
        {
            TagLib.File file = StreamTagger.ProcessUri(uri);            
            return file.Tag.Pictures;            
        }
        
        protected int GetPictureIndexToUse(IPicture [] pictures)
        {
            int preferred_index = -1;
            
            // Use the front cover.  If we don't find it, use the first image. 
            for(int i = 0; i < pictures.Length; i++) {
                if(preferred_index == -1) {
                    preferred_index = i;
                }
                
                if(pictures[i].Type == PictureType.FrontCover) {
                    preferred_index = i;
                    break;
                }
            }
            
            return preferred_index;
        }
        
        protected bool SavePicture(IPicture picture, string image_path)
        {
            if(picture == null || picture.Data == null || picture.Data.Count == 0) {
                return false;
            }
            
            using(System.IO.MemoryStream stream = new System.IO.MemoryStream(picture.Data.Data)) {
                Gdk.Pixbuf pixbuf = new Gdk.Pixbuf(stream);
                pixbuf.Save(image_path, "jpeg");
            }
            
            return true;
        }
    }
}
