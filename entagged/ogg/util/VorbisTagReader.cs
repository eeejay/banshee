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
 * Revision 1.1  2005/06/27 00:47:27  abock
 * Added entagged-sharp
 *
 * Revision 1.4  2005/02/08 12:54:40  kikidonk
 * Added cvs log and header
 *
 */

using System.IO;
using Entagged.Audioformats.exceptions;

namespace Entagged.Audioformats.Ogg.Util {
	public class VorbisTagReader {
		
		private OggTagReader oggTagReader = new OggTagReader();
		
		public Tag Read( Stream raf ) {
			long oldPos = 0;
			//----------------------------------------------------------
			
			//Check wheter we have an ogg stream---------------
			raf.Seek( 0 , SeekOrigin.Begin);
			byte[] b = new byte[4];
			raf.Read(b, 0, b.Length);
			
			string ogg = new string(System.Text.Encoding.ASCII.GetChars(b));
			if( ogg != "OggS" )
				throw new CannotReadException("OggS Header could not be found, not an ogg stream");
			//--------------------------------------------------
			
			//Parse the tag ------------------------------------
			raf.Seek( 0 , SeekOrigin.Begin);

			//Supposing 1st page = codec infos
			//			2nd page = comment+decode info
			//...Extracting 2nd page
			
			//1st page to get the length
			b = new byte[4];
			oldPos = raf.Position;
			raf.Seek(26, SeekOrigin.Begin);
			int pageSegments = raf.ReadByte()&0xFF; //unsigned
			raf.Seek(oldPos, SeekOrigin.Begin);
			
			b = new byte[27 + pageSegments];
			raf.Read( b , 0,  b .Length);

			OggPageHeader pageHeader = new OggPageHeader( b );

			raf.Seek( raf.Position + pageHeader.PageLength , SeekOrigin.Begin);

			//2nd page extraction
			oldPos = raf.Position;
			raf.Seek(raf.Position + 26, SeekOrigin.Begin);
			pageSegments = raf.ReadByte()&0xFF; //unsigned
			raf.Seek(oldPos, SeekOrigin.Begin);
			
			b = new byte[27 + pageSegments];
			raf.Read( b , 0,  b .Length);
			pageHeader = new OggPageHeader( b );

			b = new byte[7];
			raf.Read( b , 0,  b .Length);
			
			string vorbis = new string(System.Text.Encoding.ASCII.GetChars(b, 1, 6));
			if(b[0] != 3 || vorbis != "vorbis")
				throw new CannotReadException("Cannot find comment block (no vorbis header)");

			//Begin tag reading
			OggTag tag = oggTagReader.Read(raf);
			
			byte isValid = (byte) raf.ReadByte();
			if ( isValid == 0 )
				throw new CannotReadException("Error: The OGG Stream isn't valid, could not extract the tag");
			
			return tag;
		}
	}
}
