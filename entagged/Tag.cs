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
 * Revision 1.4  2005/07/20 02:34:04  abock
 * Updates to entagged
 *
 * Revision 1.4  2005/02/25 15:31:16  kikidonk
 * Big structure change
 *
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.Collections;
using System.Text;
using Entagged.Audioformats.Util;

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

		public static string[] Genres 
		{
			get { return DEFAULT_GENRES; }
		}
	}

	////////////////////////////////////////////////////////
	//  
	//  Tag: Defines the core-interface for a Tag in 
	//  an audio file.
	//
	///////////////////////////////////////////////////////

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
        
        bool SetEncoding(string enc);
	}

	////////////////////////////////////////////////////////
	//  
	//  AbstractTag:  Defines basic operations on a Tag
	//
	///////////////////////////////////////////////////////

	public abstract class AbstractTag : Tag, IEnumerable {

		protected Hashtable fields = new Hashtable();
		protected int commonNumber = 0;
		
		public IList Title 
		{
			get { return Get(TitleId); }
		}
		public IList Album 
		{
			get { return Get(AlbumId); }
		}
		public IList Artist 
		{
			get { return Get(ArtistId); }
		}
		public IList Genre 
		{
			get { return Get(GenreId); }
		}
		public IList Track 
		{
			get { return Get(TrackId); }
		}
		public IList Year 
		{
			get { return Get(YearId); }
		}
		public IList Comment 
		{
			get { return Get(CommentId); }
		}
	    
		public string FirstTitle 
		{
			get {
				IList l = Get(TitleId);
				return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
			}
		}
		public string FirstAlbum 
		{
			get {
				IList l =  Get(AlbumId);
				return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
			}
		}

		public string FirstArtist 
		{
			get {
				IList l =  Get(ArtistId);
				return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
			}
		}
		
		public string FirstGenre 
		{
			get {
				IList l =  Get(GenreId);
				return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
			}
		}
		
		public string FirstTrack 
		{
			get {
				IList l =  Get(TrackId);
				return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
			}
		}
		
		public string FirstYear 
		{
			get {
				IList l =  Get(YearId);
				return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
			}
		}
		
		public string FirstComment 
		{
			get {
				IList l =  Get(CommentId);
				return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
			}
		}

	    
		public void SetTitle(string s) 
		{
			Set (CreateTitleField (s));
		}
		public void SetAlbum(string s) 
		{
			Set (CreateAlbumField (s));
		}
		public void SetArtist(string s) 
		{
			Set (CreateArtistField (s));
		}
		public void SetGenre(string s) 
		{
			Set (CreateGenreField (s));
		}
		public void SetTrack(string s) 
		{
			Set (CreateTrackField (s));
		}
		public void SetYear(string s) 
		{
			Set (CreateYearField (s));
		}
		public void SetComment(string s) 
		{
			Set( CreateCommentField (s));
		}
	    
		public void AddTitle(string s) 
		{
			Add (CreateTitleField (s));
		}
		
		public void AddAlbum(string s) 
		{
			Add( CreateAlbumField (s));
		}
		public void AddArtist(string s) 
		{
			Add (CreateArtistField (s));
		}
		public void AddGenre(string s) 
		{
			Add (CreateGenreField (s));
		}
		public void AddTrack(string s) 
		{
			Add (CreateTrackField (s));
		}
		public void AddYear(string s) 
		{
			Add (CreateYearField (s));
		}
		public void AddComment(string s) 
		{
			Add (CreateCommentField (s));
		}
	    
		public bool HasField(string id) 
		{
			return Get(id).Count != 0; 
		}
		public bool IsEmpty 
		{
			get { return fields.Count == 0; }
		}
		public bool HasCommonFields 
		{
			get { return commonNumber != 0; }
		}
	    
		private class FieldsEnumerator : IEnumerator {
	    	
			private IEnumerator field;
			private IDictionaryEnumerator it;
	        
			public FieldsEnumerator(IDictionaryEnumerator it) 
			{
				this.it = it;
	    		
				bool fieldMoved = it.MoveNext();
				//If no elements at first level , return false
				if(!fieldMoved)
					this.field = null;
				else
					//We set field to the first list iterator
					field = (( (DictionaryEntry) it.Current).Value as IList).GetEnumerator();
			}
	    	
			public object Current 
			{
				get { return field.Current; }
			}

			public bool MoveNext() 
			{
				if(field == null)
					return false;
					
				bool listMoved = field.MoveNext();
				while(!listMoved) {
					bool fieldMoved = it.MoveNext();
			    	
					//If no elements at first level , return false
					if(!fieldMoved)
						return false;
			    	
					//We set field to the first list iterator
					field = (( (DictionaryEntry) it.Current).Value as IList).GetEnumerator();
					listMoved = field.MoveNext();
				}
				          	
				return listMoved;
			}
	            	            
			public void Reset() 
			{
			}
		}
	    
		public IEnumerator GetEnumerator() 
		{
			return new FieldsEnumerator( fields.GetEnumerator() );
		}
	        
		public IList Get (string id) 
		{
			IList list = fields[id] as IList;
			
			if(list == null)
				return new ArrayList();
			
			return list;
		}
		public void Set (TagField field) 
		{
			if(field == null)
				return;
			
			//If an empty field is passed, we delete all the previous ones
			if(field.IsEmpty) {
				object removed = fields[field.Id];
				fields.Remove(field.Id);
			    
				if(removed != null && field.IsCommon)
					commonNumber--;
				return;
			}
			
			//If there is already an existing field with same id
			//and both are TextFields, we update the first element
			IList l = fields[field.Id] as IList;
			if(l != null) {
				TagField f = l[0] as TagField;
				f.CopyContent(field);
				return;
			}
			
			//Else we put the new field in the fields.
			l = new ArrayList();
			l.Add(field);
			fields[field.Id] =  l;
			if(field.IsCommon)
				commonNumber++;
		}

		public void Add(TagField field) 
		{
			if(field == null || field.IsEmpty)
				return;
			
			IList list = fields[field.Id] as IList;
			
			//There was no previous item
			if(list == null) {
				list = new ArrayList();
				list.Add(field);
				fields[field.Id] = list;
				if(field.IsCommon)
					commonNumber++;
			}
			else {
				//We append to existing list
				list.Add(field);
			}
		}
	    
		public override string ToString() 
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("Tag content:\n");
			foreach(TagField field in this) {
				sb.Append("\t");
				sb.Append(field.Id);
				sb.Append(" : ");
				sb.Append(field.ToString());
				sb.Append("\n");
			}
			return sb.ToString().Substring(0,sb.Length-1);
		}
	    
		public void Merge(Tag tag) 
		{
			//FIXME: Improve me, for the moment,
			//it overwrites this tag with other values
			//FIXME: TODO: an abstract method that merges particular things for each 
			//format
			if( Title.Count == 0)
				SetTitle(tag.FirstTitle);
			if( Artist.Count == 0 )
				SetArtist(tag.FirstArtist);
			if( Album.Count == 0 )
				SetAlbum(tag.FirstAlbum);
			if( Year.Count == 0 )
				SetYear(tag.FirstYear);
			if( Comment.Count == 0 )
				SetComment(tag.FirstComment);
			if( Track.Count == 0 )
				SetTrack(tag.FirstTrack);
			if( Genre.Count == 0 )
				SetGenre(tag.FirstGenre);
		}
	    
        public bool SetEncoding(string enc) {
            if(!IsAllowedEncoding(enc)) {
                return false;
            }
            
            foreach(TagField field in this) {
                if(field is TagTextField) {
                    (field as TagTextField).Encoding = enc;
                }
            }
            
            return true;
        }
		//--------------------------------
		protected abstract string ArtistId {
			get;
		}
		protected abstract string AlbumId {
			get;
		}
		protected abstract string TitleId {
			get;
		}
		protected abstract string TrackId {
			get;
		}
		protected abstract string YearId {
			get;
		}
		protected abstract string CommentId {
			get;
		}
		protected abstract string GenreId {
			get;
		}
	    
		protected abstract TagField CreateArtistField(string content);
		protected abstract TagField CreateAlbumField(string content);
		protected abstract TagField CreateTitleField(string content);
		protected abstract TagField CreateTrackField(string content);
		protected abstract TagField CreateYearField(string content);
		protected abstract TagField CreateCommentField(string content);
		protected abstract TagField CreateGenreField(string content);
	    
		protected abstract bool IsAllowedEncoding(string enc);
		//---------------------------------------
	}

	////////////////////////////////////////////////////////
	//  
	//  GenericTag:  Defines a basic mapping of Tags between
	//  different audio file formats
	//
	///////////////////////////////////////////////////////

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
		    
			public override string ToString() 
			{
				return Id + " : " + Content;
			}
		    
			public void CopyContent(TagField field) 
			{
				if(field is TagTextField) {
					this.content = (field as TagTextField).Content;
				}
			}
		}
	}
}
