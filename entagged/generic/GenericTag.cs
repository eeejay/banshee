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
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

namespace Entagged.Audioformats.Generic {

	public class GenericTag : AbstractTag {
	    private static string[] keys = 
		{
	        "ARTIST",
	        "ALBUM",
	        "TITLE",
	        "TRACK",
	        "YEAR",
	        "GENRE",
	        "COMMENT"
		};

		public static int ARTIST = 0;
		public static int ALBUM = 1;
		public static int TITLE = 2;
		public static int TRACK = 3;
		public static int YEAR = 4;
		public static int GENRE = 5;
		public static int COMMENT = 6;
			
		protected override string ArtistId {
		    get { return keys[ARTIST]; }
		}
		protected override string AlbumId {
		    get { return keys[ALBUM]; }
		}
		protected override string TitleId {
		    get { return keys[TITLE]; }
		}
		protected override string TrackId {
		    get { return keys[TRACK]; }
		}
		protected override string YearId {
		    get { return keys[YEAR]; }
		}
		protected override string CommentId {
		    get { return keys[COMMENT]; }
		}
		protected override string GenreId {
		    get { return keys[GENRE]; }
		}
		
		protected override TagField CreateArtistField(string content) {
		    return new GenericTagTextField(keys[ARTIST], content);
		}
		protected override TagField CreateAlbumField(string content) {
		    return new GenericTagTextField(keys[ALBUM], content);
		}
		protected override TagField CreateTitleField(string content) {
		    return new GenericTagTextField(keys[TITLE], content);
		}
		protected override TagField CreateTrackField(string content) {
		    return new GenericTagTextField(keys[TRACK], content);
		}
		protected override TagField CreateYearField(string content) {
		    return new GenericTagTextField(keys[YEAR], content);
		}
		protected override TagField CreateCommentField(string content) {
		    return new GenericTagTextField(keys[COMMENT], content);
		}
		protected override TagField CreateGenreField(string content) {
		    return new GenericTagTextField(keys[GENRE], content);
		}
		
		protected override bool IsAllowedEncoding(string enc) {
		    return true;
		}
		
		private class GenericTagTextField : TagTextField {
		    
		    private string id;
		    private string content;
		    
		    public GenericTagTextField(string id, string content) {
		        this.id = id;
		        this.content = content;
		    }
		    
		    public string Content {
		        get { return this.content; }
		        set { this.content = value; }
		    }

		    public string Encoding {
		        get { return "ISO-8859-1"; }
		        set { /* Not allowed */}
		    }

		    public string Id {
		        get { return id; }
		    }

		    public byte[] RawContent {
		        /* FIXME: What to do here ? not supported */
		        get { return new byte[] {}; }
		    }

		    public bool IsBinary {
		        get { return false; }
		        set { /* Not Supported */ }
		    }

		    public bool IsCommon {
		        get { return true; }
		    }

		    public bool IsEmpty {
		        get { return content == ""; } 
		    }
		    
		    public override string ToString() {
		        return Id + " : " + Content;
		    }
		    
		    public void CopyContent(TagField field) {
		        if(field is TagTextField) {
		            this.content = (field as TagTextField).Content;
		        }
		    }
		}
	}
}
