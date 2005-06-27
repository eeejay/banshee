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

using System.Collections;
using System.Text;

namespace Entagged.Audioformats.Generic {

	public abstract class AbstractTag : Tag,IEnumerable {

		protected Hashtable fields = new Hashtable();
		protected int commonNumber = 0;
		
		public IList Title {
	        get { return Get(TitleId); }
	    }
	    public IList Album {
	        get { return Get(AlbumId); }
	    }
	    public IList Artist {
	        get { return Get(ArtistId); }
	    }
	    public IList Genre {
	        get { return Get(GenreId); }
	    }
	    public IList Track {
	        get { return Get(TrackId); }
	    }
	    public IList Year {
	        get { return Get(YearId); }
	    }
	    public IList Comment {
	        get { return Get(CommentId); }
	    }
	    
	    public string FirstTitle {
	        get {
	        	IList l = Get(TitleId);
	       		return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
	        }
	    }
	    public string FirstAlbum {
	        get {
	        	IList l =  Get(AlbumId);
	       		return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
	        }
	    }
	    public string FirstArtist {
	        get {
	        	IList l =  Get(ArtistId);
	        	return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
	        }
	    }
	    public string FirstGenre {
	        get {
	        	IList l =  Get(GenreId);
	        	return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
	        }
	    }
	    public string FirstTrack {
	        get {
	        	IList l =  Get(TrackId);
	        	return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
	        }
	    }
	    public string FirstYear {
	        get {
	        	IList l =  Get(YearId);
	        	return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
	        }
	    }
	    public string FirstComment {
	        get {
	        	IList l =  Get(CommentId);
	        	return (l.Count != 0) ? (l[0] as TagTextField).Content : "";
	        }
	    }

	    
	    public void SetTitle(string s) {
	        Set(CreateTitleField(s));
	    }
	    public void SetAlbum(string s) {
	        Set(CreateAlbumField(s));
	    }
	    public void SetArtist(string s) {
	        Set(CreateArtistField(s));
	    }
	    public void SetGenre(string s) {
	        Set(CreateGenreField(s));
	    }
	    public void SetTrack(string s) {
	        Set(CreateTrackField(s));
	    }
	    public void SetYear(string s) {
	        Set(CreateYearField(s));
	    }
	    public void SetComment(string s) {
	        Set(CreateCommentField(s));
	    }
	    
	    
	    public void AddTitle(string s) {
	        Add(CreateTitleField(s));
	    }
	    public void AddAlbum(string s) {
	        Add(CreateAlbumField(s));
	    }
	    public void AddArtist(string s) {
	        Add(CreateArtistField(s));
	    }
	    public void AddGenre(string s) {
	        Add(CreateGenreField(s));
	    }
	    public void AddTrack(string s) {
	        Add(CreateTrackField(s));
	    }
	    public void AddYear(string s) {
	        Add(CreateYearField(s));
	    }
	    public void AddComment(string s) {
	        Add(CreateCommentField(s));
	    }
	    
	    
	    public bool HasField(string id) {
	        return Get(id).Count != 0; 
	    }
	    public bool IsEmpty {
	        get { return fields.Count == 0; }
	    }
	    public bool HasCommonFields {
	        get { return commonNumber != 0; }
	    }
	    
	    private class FieldsEnumerator : IEnumerator {
	    	
	    	private IEnumerator field;
	        private IDictionaryEnumerator it;
	        
	    	public FieldsEnumerator(IDictionaryEnumerator it) {
	    		this.it = it;
	    		
		    	bool fieldMoved = it.MoveNext();
		    	//If no elements at first level , return false
		    	if(!fieldMoved)
		    		this.field = null;
		    	else
		    		//We set field to the first list iterator
					field = (( (DictionaryEntry) it.Current).Value as IList).GetEnumerator();
	    	}
	    	
			public object Current {
				get { return field.Current; }
			}

			public bool MoveNext() {
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
	            	            
			public void Reset() {
			}
		}
	    
		public IEnumerator GetEnumerator() {
	        return new FieldsEnumerator( fields.GetEnumerator() );
	    }
	        
	    
	    public IList Get(string id) {
			IList list = fields[id] as IList;
			
			if(list == null)
			    return new ArrayList();
			
			return list;
		}
	    public void Set(TagField field) {
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
	    public void Add(TagField field) {
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
	    
	    public override string ToString() {
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
	    
	    public void Merge(Tag tag) {
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
}
