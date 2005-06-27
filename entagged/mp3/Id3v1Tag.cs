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
 * Revision 1.3  2005/02/08 12:54:42  kikidonk
 * Added cvs log and header
 *
 */

using Entagged.Audioformats;
using Entagged.Audioformats.Generic;

namespace Entagged.Audioformats.Mp3 {

	public class Id3v1Tag : GenericTag {
		public static string[] Genres {
			get { return TagGenres.Genres; }
		}
		
		protected override bool IsAllowedEncoding(string enc) {
		    return enc == "ISO-8859-1";
		}
		
		public string TranslateGenre( byte b) {
			int i = b & 0xFF;

			if ( i == 255 || i > Genres.Length - 1 )
				return "";
			return Genres[i];
		}
		
		public override string ToString() {
			return "Id3v1 "+base.ToString();
		}
	}
}
