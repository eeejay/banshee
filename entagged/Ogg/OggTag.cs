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
 * Revision 1.2  2005/07/20 02:34:08  abock
 * Updates to entagged
 *
 * Revision 1.3  2005/02/08 12:54:42  kikidonk
 * Added cvs log and header
 *
 */

using Entagged.Audioformats.Util;
using Entagged.Audioformats.Ogg.Util;

namespace Entagged.Audioformats.Ogg {
	public class OggTag : AbstractTag {

	    private string vendor = "";
	    //This is the vendor string that will be written if no other is supplied
		public const string DEFAULT_VENDOR = "Entagged - The Musical Box";

	    protected override TagField CreateAlbumField(string content) {
	        return new OggTagField("ALBUM", content);
	    }

	    protected override TagField CreateArtistField(string content) {
	        return new OggTagField("ARTIST", content);
	    }

	    protected override TagField CreateCommentField(string content) {
	        return new OggTagField("DESCRIPTION", content);
	    }

	    protected override TagField CreateGenreField(string content) {
	        return new OggTagField("GENRE", content);
	    }

	    protected override TagField CreateTitleField(string content) {
	        return new OggTagField("TITLE", content);
	    }

	    protected override TagField CreateTrackField(string content) {
	        return new OggTagField("TRACKNUMBER", content);
	    }

	    protected override TagField CreateYearField(string content) {
	        return new OggTagField("DATE", content);
	    }

	    protected override string AlbumId {
	        get { return "ALBUM"; }
	    }

	    protected override string ArtistId {
	        get { return "ARTIST"; }
	    }

	    protected override string CommentId {
	        get { return "DESCRIPTION"; }
	    }

	    protected override string GenreId {
	        get { return "GENRE"; }
	    }
	
	    protected override string TitleId {
	        get { return "TITLE"; }
	    }

	    protected override string TrackId {
	        get { return "TRACKNUMBER"; }
	    }

	    protected override string YearId {
	        get { return "DATE"; }
	    }
		
		public string Vendor {
			get {
				if( this.vendor.Trim() != "" )
				    return vendor;
		    
				return DEFAULT_VENDOR;
			}
			set {
				if(value == null)
	            	this.vendor = "";
	        	else
	        		this.vendor = value;
	        }
	    }
	    
	    protected override bool IsAllowedEncoding(string enc) {
	        return enc == "UTF-8";
	    }
		
	    public override string ToString() {
	        return "OGG " + base.ToString();
	    }
	}
}
