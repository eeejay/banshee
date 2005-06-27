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
 * Revision 1.1  2005/06/27 00:47:22  abock
 * Added entagged-sharp
 *
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.Collections;
using Entagged.Audioformats.Generic;

namespace Entagged.Audioformats {

	public class TagGenres {
		private static string[] DEFAULT_GENRES = { "Blues", "Classic Rock",
	            "Country", "Dance", "Disco", "Funk", "Grunge", "Hip-Hop", "Jazz",
	            "Metal", "New Age", "Oldies", "Other", "Pop", "R&B", "Rap",
	            "Reggae", "Rock", "Techno", "Industrial", "Alternative", "Ska",
	            "Death Metal", "Pranks", "Soundtrack", "Euro-Techno", "Ambient",
	            "Trip-Hop", "Vocal", "Jazz+Funk", "Fusion", "Trance", "Classical",
	            "Instrumental", "Acid", "House", "Game", "Sound Clip", "Gospel",
	            "Noise", "AlternRock", "Bass", "Soul", "Punk", "Space",
	            "Meditative", "Instrumental Pop", "Instrumental Rock", "Ethnic",
	            "Gothic", "Darkwave", "Techno-Industrial", "Electronic",
	            "Pop-Folk", "Eurodance", "Dream", "Southern Rock", "Comedy",
	            "Cult", "Gangsta", "Top 40", "Christian Rap", "Pop/Funk", "Jungle",
	            "Native American", "Cabaret", "New Wave", "Psychadelic", "Rave",
	            "Showtunes", "Trailer", "Lo-Fi", "Tribal", "Acid Punk",
	            "Acid Jazz", "Polka", "Retro", "Musical", "Rock & Roll",
	            "Hard Rock", "Folk", "Folk-Rock", "National Folk", "Swing",
	            "Fast Fusion", "Bebob", "Latin", "Revival", "Celtic", "Bluegrass",
	            "Avantgarde", "Gothic Rock", "Progressive Rock",
	            "Psychedelic Rock", "Symphonic Rock", "Slow Rock", "Big Band",
	            "Chorus", "Easy IListening", "Acoustic", "Humour", "Speech",
	            "Chanson", "Opera", "Chamber Music", "Sonata", "Symphony",
	            "Booty Bass", "Primus", "Porn Groove", "Satire", "Slow Jam",
	            "Club", "Tango", "Samba", "Folklore", "Ballad", "Power Ballad",
	            "Rhythmic Soul", "Freestyle", "Duet", "Punk Rock", "Drum Solo",
	            "A capella", "Euro-House", "Dance Hall" };

		public static string[] Genres {
			get { return DEFAULT_GENRES; }
		}
	}

	public interface Tag : IEnumerable {
	    
	    void Add(TagField field);

	    void AddAlbum(string s);
	    void AddArtist(string s);
	    void AddComment(string s);
	    void AddGenre(string s);
	    void AddTitle(string s);
	    void AddTrack(string s);
	    void AddYear(string s);

	    IList Get(string id);


	    IList Genre {
	    	get;
	    }
	    IList Title {
	    	get;
	    }
	    IList Track {
	    	get;
	    }
	    IList Year {
	    	get;
	    }
	    IList Album {
	    	get;
	    }
	    IList Artist {
	    	get;
	    }
	    IList Comment {
	    	get;
	    }
	    
	    string FirstGenre {
	    	get;
	    }
	    string FirstTitle {
	    	get;
	    }
	    string FirstTrack {
	    	get;
	    }
	    string FirstYear {
	    	get;
	    }
	    string FirstAlbum {
	    	get;
	    }
	    string FirstArtist {
	    	get;
	    }
	    string FirstComment {
	    	get;
	    }
	    
	    bool HasCommonFields {
	    	get;
	    }
	    bool HasField(string id);
	    bool IsEmpty {
	    	get;
	    }

	    void Merge(Tag tag);
	    
	    void Set(TagField field);

	    void SetAlbum(string s);
	    void SetArtist(string s);
	    void SetComment(string s);
	    void SetGenre(string s);
	    void SetTitle(string s);
	    void SetTrack(string s);
	    void SetYear(string s);
	}
}
