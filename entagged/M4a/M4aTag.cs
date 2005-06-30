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

using Entagged.Audioformats.Util;
using Entagged.Audioformats.M4a.Util;
 
namespace Entagged.Audioformats.M4a
{
	public class M4aTag : AbstractTag
	{
		protected override TagField CreateAlbumField(string content) 
		{
			return new M4aTagField(AlbumId, content);
		}

		protected override TagField CreateArtistField(string content) 
		{
			return new M4aTagField(ArtistId, content);
		}

		protected override TagField CreateCommentField(string content) 
		{
			return new M4aTagField(CommentId, content);
		}

		protected override TagField CreateGenreField(string content) 
		{
			return new M4aTagField(GenreId, content);
		}

		protected override TagField CreateTitleField(string content) 
		{
			return new M4aTagField(TitleId, content);
		}

		protected override TagField CreateTrackField(string content) 
		{
			return new M4aTagField(TrackId, content);
		}

		protected override TagField CreateYearField(string content) 
		{
			return new M4aTagField(YearId, content);
		}

		protected override string AlbumId 
		{
			get { 
				return "ALBUM"; 	
			}
		}

		protected override string ArtistId 
		{
			get { 
				return "ARTIST"; 
			}
		}

		protected override string CommentId 
		{
			get { 
				return "COMMENTS"; 
			}
		}

		protected override string GenreId 
		{
			get {
				return "GENRE"; 
			}
		}

		protected override string TitleId 
		{
			get { 
				return "TITLE"; 
			}
		}

		protected override string TrackId 
		{
			get { 
				return "TRACKNUMBER"; 
			}
		}

		protected override string YearId 
		{
			get { 
				return "YEAR"; 
			}
		}

		protected override bool IsAllowedEncoding(string enc) 
		{
			return enc == "UTF-8";
		}

		public override string ToString() 
		{
			return "OGG " + base.ToString();
		}
	}
}
