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
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

namespace Entagged.Audioformats.Ape.Util {
	public class WavRIFFHeader {
		
		private bool isValid = false;

		public WavRIFFHeader( byte[] b ) {

			string RIFF = new string(System.Text.Encoding.ASCII.GetChars(b,0,4));

			string WAVE = new string(System.Text.Encoding.ASCII.GetChars(b,8,4));

			if(RIFF == "RIFF" && WAVE == "WAVE") {
				isValid = true;
			}
			
		}

		public bool Valid {
			get { return isValid; }
		}

		public override string ToString() {
			string s = "RIFF-WAVE Header:\n";
			s += "Is valid?: " + isValid;
			return s;
		}
	}
}
