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
 * Revision 1.4  2005/08/02 05:24:57  abock
 * Sonance 0.8 Updates, Too Numerous, see ChangeLog
 *
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.IO;
using Entagged.Audioformats.Exceptions;

namespace Entagged.Audioformats.Mp3.Util {
	public class Id3v1TagReader {
		public Id3v1Tag Read( Stream mp3Stream )
		{
			Id3v1Tag tag = new Id3v1Tag();
			//Check wether the file contains an Id3v1 tag--------------------------------
			mp3Stream.Seek( -128 , SeekOrigin.End);
			
			byte[] b = new byte[3];
			mp3Stream.Read( b, 0, 3 );
			mp3Stream.Seek(0, SeekOrigin.Begin);
			string tagS = new string(System.Text.Encoding.ASCII.GetChars( b ));
			if(tagS != "TAG"){
				throw new CannotReadException("There is no Id3v1 Tag in this file");
			}
			
			mp3Stream.Seek( - 128 + 3, SeekOrigin.End );
			//Parse the tag -)------------------------------------------------
			string songName = Read(mp3Stream, 30);
			//------------------------------------------------
			string artist = Read(mp3Stream, 30);
			//------------------------------------------------
			string album = Read(mp3Stream, 30);
			//------------------------------------------------
			string year = Read(mp3Stream, 4);
			//------------------------------------------------
			string comment = Read(mp3Stream, 30);
			//------------------------------------------------
			string trackNumber = "";
			
			mp3Stream.Seek(- 2, SeekOrigin.Current);
			b = new byte[2];
			mp3Stream.Read(b, 0, 2);
			
			if ( b[0] == 0 ) {
				trackNumber = b[1].ToString ();
			}
			//------------------------------------------------
			byte genreByte = (byte) mp3Stream.ReadByte();
			mp3Stream.Seek(0, SeekOrigin.Begin);

			tag.SetTitle( songName );
			tag.SetArtist( artist );
			tag.SetAlbum( album );
			tag.SetYear( year );
			tag.SetComment( comment );
			tag.SetTrack( trackNumber );
			tag.SetGenre( tag.TranslateGenre(genreByte) );

		
			return tag;
		}
		
		private string Read(Stream mp3Stream, int length)
		{
			byte[] b = new byte[length];
			mp3Stream.Read( b, 0, b.Length );
			string ret = new string(System.Text.Encoding.GetEncoding("ISO-8859-1").GetChars( b )).Trim();
			
			return ret.Split('\0')[0];
		}
	}
}
