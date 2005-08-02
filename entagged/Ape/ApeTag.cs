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
 * Revision 1.4  2005/08/02 05:24:54  abock
 * Sonance 0.8 Updates, Too Numerous, see ChangeLog
 *
 * Revision 1.3  2005/02/08 12:54:42  kikidonk
 * Added cvs log and header
 *
 */

using System.IO;
using Entagged.Audioformats.Util;
using Entagged.Audioformats.Ape.Util;

namespace Entagged.Audioformats.Ape {
	public class ApeTag : AbstractTag {
		
		protected override string ArtistId {
		    get { return "Artist"; }
		}
	    protected override string AlbumId {
		    get { return "Album"; }
		}
	    protected override string TitleId {
		    get { return "Title"; }
		}
	    protected override string TrackId {
		    get { return "Track"; }
		}
	    protected override string YearId {
		    get { return "Year"; }
		}
	    protected override string CommentId {
		    get { return "Comment"; }
		}
	    protected override string GenreId {
		    get { return "Genre"; }
		}
	    
	    protected override TagField CreateArtistField(string content) {
	        return new ApeTagTextField("Artist", content);
	    }
	    protected override TagField CreateAlbumField(string content) {
	        return new ApeTagTextField("Album", content);
	    }
	    protected override TagField CreateTitleField(string content) {
	        return new ApeTagTextField("Title", content);
	    }
	    protected override TagField CreateTrackField(string content) {
	        return new ApeTagTextField("Track", content);
	    }
	    protected override TagField CreateYearField(string content) {
	        return new ApeTagTextField("Year", content);
	    }
	    protected override TagField CreateCommentField(string content) {
	        return new ApeTagTextField("Comment", content);
	    }
	    protected override TagField CreateGenreField(string content) {
	        return new ApeTagTextField("Genre", content);
	    }
	    
	    protected override bool IsAllowedEncoding(string enc) {
	        return enc == "UTF-8";
	    }
	    
		public override string ToString() {
			return "APE "+base.ToString();
		}
	}
}
