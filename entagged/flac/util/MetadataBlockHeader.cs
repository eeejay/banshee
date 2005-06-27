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
 * Revision 1.3  2005/02/08 12:54:40  kikidonk
 * Added cvs log and header
 *
 */

namespace Entagged.Audioformats.Flac.Util {
	public class MetadataBlockHeader {			
		public const int STREAMINFO=0, PADDING=1, APPLICATION=2, SEEKTABLE=3, VORBIS_COMMENT=4, CUESHEET=5, UNKNOWN=6;
		private int blockType, dataLength;
		private bool lastBlock;
		private byte[] data;
		/*private byte[] bytes;*/

		public MetadataBlockHeader (byte[] b) {
			//this.bytes = b;
			
			lastBlock = ( (b[0] & 0x80) >> 7 ) == 1;
			
			int type = b[0] & 0x7F;
			switch (type) {
				case 0: blockType = STREAMINFO; break;
				case 1: blockType = PADDING; break;
				case 2: blockType = APPLICATION; break;
				case 3: blockType = SEEKTABLE; break;
				case 4: blockType = VORBIS_COMMENT; break;
				case 5: blockType = CUESHEET; break;
				default: blockType = UNKNOWN; break;
			}
			
			dataLength = (u (b[1])<<16) + (u (b[2])<<8) + (u (b[3]));
			
			data = new byte[4];
			data[0] = (byte) (data[0] & 0x7F);
			for (int i = 1; i < 4; i ++) {
				data[i] = b[i];
			}
		}

		public int DataLength {
			get {
				return dataLength;
			}
		}

		public int BlockType {
			get {
				return blockType;
			}
		}

		public string BlockTypeString {
			get {
				switch (blockType) {
					case 0: return "STREAMINFO";
					case 1: return "PADDING";
					case 2: return "APPLICATION";
					case 3: return "SEEKTABLE";
					case 4: return "VORBIS_COMMENT";
					case 5: return "CUESHEET";
					default: return "UNKNOWN-RESERVED";
				}
			}
		}

		public bool IsLastBlock {
			get {
				return lastBlock;
			}
		}

		public byte[] Data {
			get {
				return data;
			}
		}

		private int u (int i) {
			return i & 0xFF;
		}
	}
}
