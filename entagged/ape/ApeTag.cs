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
 * Revision 1.1  2005/06/27 00:47:23  abock
 * Added entagged-sharp
 *
 * Revision 1.3  2005/02/08 12:54:42  kikidonk
 * Added cvs log and header
 *
 */

using System.IO;
using Entagged.Audioformats.Generic;
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
