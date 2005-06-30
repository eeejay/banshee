/***************************************************************************
 *  Copyright 2005 Raphaël Slinckx <raphael@slinckx.net> 
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

/*
 * $Log$
 * Revision 1.1  2005/06/30 16:56:23  abock
 * New entagged
 *
 * Revision 1.3  2005/02/08 12:54:42  kikidonk
 * Added cvs log and header
 *
 */

using Entagged.Audioformats;
using Entagged.Audioformats.Util;
using Entagged.Audioformats.Mp3.Util.Id3Frames;

namespace Entagged.Audioformats.Mp3 {

	public class Id3v1Tag : GenericTag {
		public static string[] Genres {
			get { return TagGenres.Genres; }
		}
		
		protected override bool IsAllowedEncoding(string enc) {
		    return enc == "ISO-8859-1";
		}
		
		public string TranslateGenre( byte b) {
			int i = b & 0xFF;

			if ( i == 255 || i > Genres.Length - 1 )
				return "";
			return Genres[i];
		}
		
		public override string ToString() {
			return "Id3v1 "+base.ToString();
		}
	}
	
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
