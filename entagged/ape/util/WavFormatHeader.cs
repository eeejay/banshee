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
	public class WavFormatHeader {
		
		private bool isValid = false;
		
		private int channels,sampleRate,bytesPerSecond,bitrate;

		public WavFormatHeader( byte[] b ) {
			string fmt = new string(System.Text.Encoding.ASCII.GetChars(b,0,3));

			if(fmt == "fmt" && b[8]==1) {
				channels = b[10];

				sampleRate = u(b[15])*16777216 + u(b[14])*65536 + u(b[13])*256 + u(b[12]);

				bytesPerSecond = u(b[19])*16777216 + u(b[18])*65536 + u(b[17])*256 + u(b[16]);

				bitrate = u(b[22]);
				
				isValid = true;
			}
			
		}

		public bool Valid {
			get { return isValid; }
		}
		
		public int ChannelNumber {
			get { return channels; }
		}
		
		public int SamplingRate {
			get { return sampleRate; }
		}
		
		public int BytesPerSecond {
			get { return bytesPerSecond; }
		}
		
		public int Bitrate {
			get { return bitrate; }
		}

		private int u( int n ) {
			return n & 0xff ;
		}
		
		public override string ToString() {
			string s = "RIFF-WAVE Header:\n";
			s += "Is valid?: " + isValid;
			return s;
		}
	}
}
