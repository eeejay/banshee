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
 * Revision 1.1  2005/06/27 00:47:26  abock
 * Added entagged-sharp
 *
 * Revision 1.3  2005/02/08 12:54:42  kikidonk
 * Added cvs log and header
 *
 */

using Entagged.Audioformats.Generic;
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
