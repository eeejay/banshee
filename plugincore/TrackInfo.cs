/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  TrackInfo.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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

using System;
using Mono.Unix;
using Gtk;
 
namespace Banshee
{
	public abstract class TrackInfo
	{
		protected string uri;
	    protected string mimetype;
	    protected string artist;
	    protected string album;
	    protected string title;
	    protected DateTime dateAdded;
	    protected int year;
	    protected string genre;
	    protected string performer;
		
		protected string asin;
		protected string label;
	    
	    protected uint rating;
	    protected uint numberOfPlays;
	    protected DateTime lastPlayed;
	    
	    protected long duration;
	    protected uint trackNumber;
	    protected uint trackCount;
	   	protected int uid;
	   	protected int trackId;
	   	
	   	protected double trackGain;
	   	protected double trackPeak;
	   	protected double albumGain;
	   	protected double albumPeak;
	   	
	   	protected bool canSaveToDatabase;
	   	protected bool canPlay = true;
	   	
	   	public Gtk.TreeIter PreviousTrack;
	   	
	   	public virtual void Save()
	   	{
	   	
	   	}
	   	
	   	public virtual void IncrementPlayCount()
	   	{
	   	
	   	}
	   	
	   	protected virtual void SaveRating()
	   	{
	   	
	   	}
	   	
	   	public int TrackId         { get { return trackId;       } }
	    public int Uid             { get { return uid;           } }
	
		public string Uri          { get { return uri;           } } 
	   	public string MimeType     { get { return mimetype;      } }
	    public string Artist       { get { return artist;        } 
	                                 set { artist = value;       } }
	    public string Album        { get { return album;         }
	                                 set { album = value;        } }
	    public string Title        { get { return title;         } 
	                                 set { title = value;        } }
	    public string Genre        { get { return genre;         } 
	                                 set { genre = value;        } }

	    public string Performer    { get { return performer;     } }
	    public int Year            { get { return year;          } 
	                                 set { year = value;         } }  
	    
	    public long Duration       { get { return duration;        } 
	                                 set { duration = value;     } }
	   	
	   	public uint TrackNumber    { get { return trackNumber;   } 
	   	                             set { trackNumber = value;  } }
	    public uint TrackCount     { get { return trackCount;    } 
	                                 set { trackCount = value;   } }
	    
	    public uint NumberOfPlays  { get { return numberOfPlays; } }
	    public DateTime LastPlayed { get { return lastPlayed;    } }
	    public DateTime DateAdded  { get { return dateAdded;     } }
		
		public uint Rating         { get { return rating;        } 
		                             set { rating = value; 
		                                   SaveRating();         } }
	    	                               
	    public double TrackGain    { get { return trackGain;     } }
	    public double TrackPeak    { get { return trackPeak;     } }
	    public double AlbumGain    { get { return albumGain;     } }
	    public double AlbumPeak    { get { return albumPeak;     } }
	    
	    public string DisplayArtist { 
	    	get { return artist == null || artist == String.Empty
	    		? Catalog.GetString("Unknown Artist") : artist; } 
	    }
	    
	    public string DisplayAlbum { 
	    	get { return album == null || album == String.Empty 
	    		? Catalog.GetString("Unknown Album") : album; } 
	    }
	    
	    public string DisplayTitle { 
	    	get { return title == null || title == String.Empty
	    		? Catalog.GetString("Unknown Title") : title; } 
	    }    	
	    
	    public bool CanSaveToDatabase {
	    	get { return canSaveToDatabase; }
	    }
	    
	    public bool CanPlay {
	    	get { return canPlay; }
	    }

		public override string ToString()
		{
			return String.Format ("{0} - {1} - {2} ({3})", artist, album, title, uri);
		}
	}	
}
