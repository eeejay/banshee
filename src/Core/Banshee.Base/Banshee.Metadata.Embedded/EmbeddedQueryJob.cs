/***************************************************************************
 *  EmbeddedQueryJob.cs
 *
 *  Copyright (C) 2006-2007 Novell, Inc.
 *  Written by Trey Ethridge <tale@juno.com>
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
using System.Collections.Generic;

using TagLib;

using Banshee.Base;
using Banshee.Metadata;
using Banshee.Collection;

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
            if(track == null || track.CoverArtFileName != null) {
                return;
            }
          
            string embedded_cover_art_file = Fetch();
            
            if(embedded_cover_art_file != null) {
                track.CoverArtFileName = embedded_cover_art_file;
            }
        }
        
        protected string Fetch()
        {
            string image_path = null;
            string artist_album_id = AlbumInfo.CreateArtistAlbumId(track.ArtistName, track.AlbumTitle, false);

            if(artist_album_id == null) {
                return null;
            }
            
            IPicture [] pictures = GetEmbeddedPictures(track.Uri);
                    
            if(pictures != null && pictures.Length > 0) {
                int preferred_index = GetPictureIndexToUse(pictures);
                IPicture picture = pictures[preferred_index];
                string path = Paths.GetCoverArtPath(artist_album_id);
                
                if(SavePicture(picture, path)) {    
                    StreamTag tag = new StreamTag();
                    tag.Name = CommonTags.AlbumCoverId;
                    tag.Value = artist_album_id;   
                    
                    AddTag(tag);
                    
                    image_path = path;
                } 
            }
            
            return image_path;
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
