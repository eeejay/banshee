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
 * Revision 1.5  2005/02/21 00:13:00  kikidonk
 * Should fix the bitrate calculation for mp3
 *
 * Revision 1.4  2005/02/18 13:38:11  kikidonk
 * Adds a isVbr method that checks wether the file is vbr or not, added check in OGG and MP3, other formats are always VBR
 *
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

namespace Entagged.Audioformats.Mp3.Util {
	public class XingMPEGFrame {

		/**  the filesize in bytes */
		private int fileSize = 0;

		/**  The number of mpeg frames in the mpeg file */
		private int frameCount = 0;

		/**  Flag to determine if it is a valid Xing Mpeg frame */
		private bool isValidXingMPEGFrame = true;

		/**  the Xing Encoding quality (0-100) */
		private int quality;

		/**  The four flags for this type of mpeg frame */
		private bool[] vbrFlags = new bool[4];

		private bool vbr = false;

		public XingMPEGFrame( byte[] bytesPart1, byte[] bytesPart2 ) {
			string xing = new string( System.Text.Encoding.ASCII.GetChars(bytesPart1, 0, 4) );

			if ( xing ==  "Xing" || xing ==  "Info" ) {
				vbr = (xing ==  "Xing");
				int[] b = u(bytesPart1);
				int[] q = u(bytesPart2);

				UpdateVBRFlags(b[7]);

				if ( vbrFlags[0] )
					frameCount = b[8] * 16777215 + b[9] * 65535 + b[10] * 255 + b[11];
				if ( vbrFlags[1] )
					fileSize = b[12] * 16777215 + b[13] * 65535 + b[14] * 255 + b[15];
				if ( vbrFlags[3] )
					quality = q[0] * 16777215 + q[1] * 65535 + q[2] * 255 + q[3];
			}
			else
				//No frame VBR MP3 XING
				isValidXingMPEGFrame = false;

		}
		
		private int[] u(byte[] b) {
			int[] i = new int[b.Length];
			for(int j = 0; j<i.Length; j++)
				i[j] = b[j] & 0xFF;
			return i;
		}

		public int FrameCount {
			get {
				if ( vbrFlags[0] )
					return frameCount;
				
				return -1;
			}
		}

		public bool Valid {
			get { return isValidXingMPEGFrame; }
		}

		public bool IsVbr {
	    	get { return vbr; }
		}
		
		public int FileSize {
		    get { return this.fileSize; }
		}

		public override string ToString() {
			string output;

			if ( isValidXingMPEGFrame ) {
				output = "\n----XingMPEGFrame--------------------\n";
				output += "Frame count:" + vbrFlags[0] + "\tFile Size:" + vbrFlags[1] + "\tQuality:" + vbrFlags[3] + "\n";
				output += "Frame count:" + frameCount + "\tFile Size:" + fileSize + "\tQuality:" + quality + "\n";
				output += "--------------------------------\n";
			}
			else
				output = "\n!!!No Valid Xing MPEG Frame!!!\n";
			return output;
		}

		private void UpdateVBRFlags(int b) {
			vbrFlags[0] = (b&0x01) == 0x01;
			vbrFlags[1] = (b&0x02) == 0x02;
			vbrFlags[2] = (b&0x04) == 0x04;
			vbrFlags[3] = (b&0x08) == 0x08;
		}
	}
} 
