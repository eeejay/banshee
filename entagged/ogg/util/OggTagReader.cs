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
using Entagged.Audioformats.Generic;

namespace Entagged.Audioformats.Ogg.Util {
	public class OggTagReader {

		public OggTag Read( Stream raf ) {
			OggTag tag = new OggTag();
			
			byte[] b = new byte[4];
			raf.Read( b , 0,  b .Length);
			int vendorstringLength = Utils.GetNumber( b, 0, 3);
			b = new byte[vendorstringLength];
			raf.Read( b , 0,  b .Length);

			tag.Vendor = new string( System.Text.Encoding.UTF8.GetChars(b) );
			
			b = new byte[4];
			raf.Read( b , 0,  b .Length);
			int userComments = Utils.GetNumber( b, 0, 3);

			for ( int i = 0; i < userComments; i++ ) {
				b = new byte[4];
				raf.Read( b , 0,  b .Length);
				int commentLength = Utils.GetNumber( b, 0, 3);
				b = new byte[commentLength];
				raf.Read( b , 0,  b .Length);
				
				OggTagField field = new OggTagField(b);
				tag.Add(field);
			}
			
			return tag;
		}
	}
}
