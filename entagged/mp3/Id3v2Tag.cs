// Copyright 2005 Raphaël Slinckx <raphael@slinckx.net> 
//
// (see http://entagged.sourceforge.net)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See
// the License for the specific language governing permissions and
// limitations under the License.

/*
 * $Log$
 * Revision 1.1  2005/06/27 00:47:25  abock
 * Added entagged-sharp
 *
 * Revision 1.4  2005/02/18 12:31:51  kikidonk
 * Adds a way to know if there was an id3 tag or not
 *
 * Revision 1.3  2005/02/08 12:54:42  kikidonk
 * Added cvs log and header
 *
 */

using Entagged.Audioformats;
using Entagged.Audioformats.Generic;
using Entagged.Audioformats.Mp3.Util.Id3Frames;

namespace Entagged.Audioformats.Mp3 {
	public class Id3v2Tag : AbstractTag {
		public static string DEFAULT_ENCODING = "UTF-16";
		
		public static byte ID3V22 = 0;
		public static byte ID3V23 = 1;
		
		private bool hasV1;
		
		protected override string ArtistId {
		    get { return "TPE1"; }
		}
	    protected override string AlbumId {
	        get { return "TALB"; }
	    }
	    protected override string TitleId {
	        get { return "TIT2"; }
	    }
	    protected override string TrackId {
	        get { return "TRCK"; }
	    }
	    protected override string YearId {
	        get { return "TYER"; }
	    }
	    protected override string CommentId {
	        get { return "COMM"; }
	    }
	    protected override string GenreId {
	        get { return "TCON"; }
	    }
	    
	    protected override TagField CreateArtistField(string content) {
	        return new TextId3Frame("TPE1", content);
	    }
	    protected override TagField CreateAlbumField(string content) {
	        return new TextId3Frame("TALB", content);
	    }
	    protected override TagField CreateTitleField(string content) {
	        return new TextId3Frame("TIT2", content);
	    }
	    protected override TagField CreateTrackField(string content) {
	        return new TextId3Frame("TRCK", content);
	    }
	    protected override TagField CreateYearField(string content) {
	        return new TextId3Frame("TYER", content);
	    }
	    protected override TagField CreateCommentField(string content) {
	        return new CommId3Frame(content);
	    }
	    protected override TagField CreateGenreField(string content) {
	        return new TextId3Frame("TCON", content);
	    }
		
	    protected override bool IsAllowedEncoding(string enc) {
	        return enc == "ISO-8859-1" ||
	        	   enc == "UTF-16";
	    }
	    
	    public bool HasId3v1 {
        	get { return hasV1; }
        	set { this.hasV1 = value; }
	    }
	    
		public override string ToString() {
			return "Id3v2 "+base.ToString();
		}
	}
}
