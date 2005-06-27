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
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

namespace Entagged.Audioformats.Mp3.Util {
	public class LameMPEGFrame {

		/**  contains the Bitrate of this frame */
		private int bitrate;

		/**  Flag indicating if bitset contains a Lame Frame */
		private bool containsLameMPEGFrame;

		/**  Contains the Filesize in bytes of the frame's File */
		private int fileSize;

		/**  Flag indicating if this is a correct Lame Frame */
		private bool isValidLameMPEGFrame = false;

		/**  Contains the Lame Version number of this frame */
		private string lameVersion;

		/**  Contains the bitset representing this Lame Frame */
		private bool containsLameFrame = false;


		/**
		 *  Creates a Lame Mpeg Frame and checks it's integrity
		 *
		 * @param  lameHeader  a byte array representing the Lame frame
		 */
		public LameMPEGFrame( byte[] lameHeader ) {
			string xing = new string( System.Text.Encoding.ASCII.GetChars(lameHeader, 0, 4) );

			if ( xing == "LAME" ) {
				isValidLameMPEGFrame = true;

				int[] b = u( lameHeader );

				containsLameFrame = ( (b[9]&0xFF) == 0xFF  );

				byte[] version = new byte[5];

				version[0] = lameHeader[4];
				version[1] = lameHeader[5];
				version[2] = lameHeader[6];
				version[3] = lameHeader[7];
				version[4] = lameHeader[8];
				lameVersion = new string( System.Text.Encoding.ASCII.GetChars(version) );

				containsLameMPEGFrame = _containsLameMPEGFrame();

				if ( containsLameMPEGFrame ) {
					bitrate = b[20];
					fileSize = b[28] * 16777215 + b[29] * 65535 + b[30] * 255 + b[31];
				}
			}
			else
				//Pas de frame VBR MP3 Lame
				isValidLameMPEGFrame = false;

		}
		
		private int[] u(byte[] b) {
			int[] i = new int[b.Length];
			for(int j = 0; j<i.Length; j++)
				i[j] = b[j] & 0xFF;
			return i;
		}


		public bool Valid {
			get { return isValidLameMPEGFrame; }
		}


		public override string ToString() {
			string output;

			if ( isValidLameMPEGFrame ) {
				output = "\n----LameMPEGFrame--------------------\n";
				output += "Lame" + lameVersion;
				if ( containsLameMPEGFrame )
					output += "\tMin.Bitrate:" + bitrate + "\tLength:" + fileSize;
				output += "\n--------------------------------\n";
			}
			else
				output = "\n!!!No Valid Lame MPEG Frame!!!\n";
			return output;
		}

		private bool _containsLameMPEGFrame() {
			return containsLameFrame;
		}
	}
}
