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
 * Revision 1.1  2005/06/27 00:47:23  abock
 * Added entagged-sharp
 *
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.IO;
using Entagged.Audioformats.exceptions;
using Entagged.Audioformats.Generic;

namespace Entagged.Audioformats.Ape.Util {
	public class ApeTagReader {

		public Tag Read(Stream raf) {
			ApeTag tag = new ApeTag();
			
			//Check wether the file contains an APE tag--------------------------------
			raf.Seek( raf.Length - 32 , SeekOrigin.Begin);
			
			byte[] b = new byte[8];
			raf.Read(b, 0, b.Length);
			
			string tagS = new string( System.Text.Encoding.ASCII.GetChars(b) );
			if(tagS != "APETAGEX" ){
				throw new CannotReadException("There is no APE Tag in this file");
			}
			//Parse the tag -)------------------------------------------------
			//Version
			b = new byte[4];
			raf.Read( b , 0,  b .Length);
			int version = Utils.GetNumber(b, 0,3);
			if(version != 2000) {
				throw new CannotReadException("APE Tag other than version 2.0 are not supported");
			}
			
			//Size
			b = new byte[4];
			raf.Read( b , 0,  b .Length);
			long tagSize = Utils.GetLongNumber(b, 0,3);

			//Number of items
			b = new byte[4];
			raf.Read( b , 0,  b .Length);
			int itemNumber = Utils.GetNumber(b, 0,3);
			
			//Tag Flags
			b = new byte[4];
			raf.Read( b , 0,  b .Length);
			//TODO handle these
			
			raf.Seek(raf.Length - tagSize, SeekOrigin.Begin);
			
			for(int i = 0; i<itemNumber; i++) {
				//Content length
				b = new byte[4];
				raf.Read( b , 0,  b .Length);
				int contentLength = Utils.GetNumber(b, 0,3);
				if(contentLength > 500000)
					throw new CannotReadException("Item size is much too large: "+contentLength+" bytes");
				
				//Item flags
				b = new byte[4];
				raf.Read( b , 0,  b .Length);
				//TODO handle these
				bool binary = ((b[0]&0x06) >> 1) == 1;
				
				int j = 0;
				while(raf.ReadByte() != 0)
					j++;
				raf.Seek(raf.Position - j -1, SeekOrigin.Begin);
				int fieldSize = j;
				
				//Read Item key
				b = new byte[fieldSize];
				raf.Read( b , 0,  b .Length);
				raf.Seek(1, SeekOrigin.Current);
				string field = new string(System.Text.Encoding.GetEncoding("ISO-8859-1").GetChars(b));
				
				//Read Item content
				b = new byte[contentLength];
				raf.Read( b , 0,  b .Length);
				if(!binary)
				    tag.Add(new ApeTagTextField(field, new string(System.Text.Encoding.UTF8.GetChars(b))));
				else
				    tag.Add(new ApeTagBinaryField(field, b));
			}
			
			return tag;
		} 
	}
}
