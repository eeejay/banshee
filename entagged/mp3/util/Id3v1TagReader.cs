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
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.IO;
using Entagged.Audioformats.exceptions;

namespace Entagged.Audioformats.Mp3.Util {
	public class Id3v1TagReader {
		public Id3v1Tag Read( Stream raf ) {
			Id3v1Tag tag = new Id3v1Tag();
			//Check wether the file contains an Id3v1 tag--------------------------------
			raf.Seek( -128 , SeekOrigin.End);
			
			byte[] b = new byte[3];
			raf.Read( b, 0, 3 );
			raf.Seek(0, SeekOrigin.Begin);
			string tagS = new string(System.Text.Encoding.ASCII.GetChars( b ));
			if(tagS != "TAG"){
				throw new CannotReadException("There is no Id3v1 Tag in this file");
			}
			
			raf.Seek( - 128 + 3, SeekOrigin.End );
			//Parse the tag -)------------------------------------------------
			string songName = Read(raf, 30);
			//------------------------------------------------
			string artist = Read(raf, 30);
			//------------------------------------------------
			string album = Read(raf, 30);
			//------------------------------------------------
			string year = Read(raf, 4);
			//------------------------------------------------
			string comment = Read(raf, 30);
			//------------------------------------------------
			string trackNumber = "";
			
			raf.Seek(- 2, SeekOrigin.Current);
			b = new byte[2];
			raf.Read(b, 0, 2);
			
			if ( b[0] == 0 ) {
				trackNumber = b[1].ToString ();
			}
			//------------------------------------------------
			byte genreByte = (byte) raf.ReadByte();
			raf.Seek(0, SeekOrigin.Begin);

			tag.SetTitle( songName );
			tag.SetArtist( artist );
			tag.SetAlbum( album );
			tag.SetYear( year );
			tag.SetComment( comment );
			tag.SetTrack( trackNumber );
			tag.SetGenre( tag.TranslateGenre(genreByte) );

		
			return tag;
		}
		
		private string Read(Stream raf, int length) {
			byte[] b = new byte[length];
			raf.Read( b, 0, b.Length );
			string ret = new string(System.Text.Encoding.GetEncoding("ISO-8859-1").GetChars( b )).Trim();
			
			return ret.Split('\0')[0];
		}
	}
}
