/***************************************************************************
 *  Copyright 2005 Novell, Inc.
 *  Aaron Bockover <aaron@aaronbock.net>
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
using Entagged.Audioformats;
using Entagged.Audioformats.Util;
 
namespace Entagged
{
	public class AudioFileWrapper
	{
		private AudioFile afb;
		private string filename;
		
		public AudioFileWrapper(string filename)
		{
			this.filename = filename;
			afb = AudioFileIO.Read(filename);
		}
		
		public int Bitrate 
		{
			get { 
				return afb.EncodingInfo.Bitrate; 
			}
		}

		public int ChannelNumber 
		{
			get { 
				return afb.EncodingInfo.ChannelNumber; 
			}
		}
		
		public string EncodingType 
		{
			get { 
				return afb.EncodingInfo.EncodingType; 
			}
		}

		public string ExtraEncodingInfo 
		{
			get { 
				return afb.EncodingInfo.ExtraEncodingInfos; 
			}
		}

		public int SamplingRate 
		{
			get { 
				return afb.EncodingInfo.SamplingRate; 
			}
		}

		public int Duration 
		{
			get { 
				return afb.EncodingInfo.Length; 
			}
		}
		
		public bool IsVbr 
		{
			get { 
				return afb.EncodingInfo.Vbr; 
			}
		}
		
		public string Genre 
		{
	    	get {
	    		return afb.Tag.FirstGenre.Equals(String.Empty) 
	    			? null : afb.Tag.FirstGenre;
	    	}
	    }
	    
	    public string Title
	    {
	    	get {
	    		return afb.Tag.FirstTitle.Equals(String.Empty) 
	    			? null : afb.Tag.FirstTitle;
	    	}
	    }
	    
	    public int TrackNumber
	    {
	    	get {
	    		try {
	    			return Convert.ToInt32(afb.Tag.FirstTrack);
	    		} catch(Exception) {
	    			return 0;
	    		}
	    	}
	    }
	    
	    public int Year
	    {
	    	get {
	    		try {
	    			return Convert.ToInt32(afb.Tag.FirstYear);
	    		} catch(Exception) {
	    			return 0;
	    		}
	    	}
	    }
	    
	    public string Album
	    {
	    	get {
	    		return afb.Tag.FirstAlbum.Equals(String.Empty) 
	    			? null : afb.Tag.FirstAlbum;
	    	}
	    }
	    
	    public string Artist
	    {
	    	get {
	    		return afb.Tag.FirstArtist.Equals(String.Empty) 
	    			? null : afb.Tag.FirstArtist;
	    	}
	    } 
	    
	    public string Comment
	    {
	    	get {
	    		return afb.Tag.FirstComment.Equals(String.Empty) 
	    			? null : afb.Tag.FirstComment;
	    	}
	    }
	    
		public Tag Tag 
		{
			get { 
				return afb.Tag == null ? new GenericTag() : afb.Tag; 
			}
		}
		
		public string Filename
		{
			get {
				return filename;
			}
		}
		
		public EncodingInfo EncodingInfo
		{
			get {
				return afb.EncodingInfo;
			}
		}
		
		public override string ToString() 
		{
			return afb.ToString();
		}
	}
}

