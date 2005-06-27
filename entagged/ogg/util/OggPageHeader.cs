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
 * Revision 1.1  2005/06/27 00:47:26  abock
 * Added entagged-sharp
 *
 * Revision 1.3  2005/02/08 12:54:40  kikidonk
 * Added cvs log and header
 *
 */

namespace Entagged.Audioformats.Ogg.Util {
	public class OggPageHeader {
		private double absoluteGranulePosition;
		private byte[] checksum;
		private byte headerTypeFlag;

		private bool isValid = false;
		private int pageLength = 0;
		private int pageSequenceNumber,streamSerialNumber;
		private byte[] segmentTable;

		public OggPageHeader( byte[] b ) {
			int streamStructureRevision = b[4];

			headerTypeFlag = b[5];

			if ( streamStructureRevision == 0 ) {
				this.absoluteGranulePosition = 0;
				for ( int i = 0; i < 8; i++ )
					this.absoluteGranulePosition += u( b[i + 6] ) * System.Math.Pow(2, 8 * i);

				streamSerialNumber = u(b[14]) + ( u(b[15]) << 8 ) + ( u(b[16]) << 16 ) + ( u(b[17]) << 24 );
				
				pageSequenceNumber = u(b[18]) + (u(b[19]) << 8 ) + ( u(b[20]) << 16 ) + ( u(b[21]) << 24 );
				
				checksum = new byte[]{b[22], b[23], b[24], b[25]};

				this.segmentTable = new byte[b.Length - 27];
				
				for ( int i = 0; i < segmentTable.Length; i++ ) {
					segmentTable[i] = b[27 + i];
					this.pageLength += u( b[27 + i] );
				}

				isValid = true;
			}
		}
		
		private int u(int i) {
			return i & 0xFF;
		}


		public double AbsoluteGranulePosition {
			get { return this.absoluteGranulePosition; }
		}


		public byte[] CheckSum {
			get { return checksum; }
		}


		public byte HeaderType {
			get { return headerTypeFlag; }
		}


		public int PageLength {
			get { return this.pageLength; }
		}
		
		public int PageSequence {
			get { return pageSequenceNumber; }
		}
		
		public int SerialNumber {
			get { return streamSerialNumber; }
		}

		public byte[] SegmentTable {
		    get { return this.segmentTable; }
		}

		public bool Valid {
			get { return isValid; }
		}

		public override string ToString() {
			string s = "Ogg Page Header:\n";

			s += "Is valid?: " + isValid + " | page length: " + pageLength + "\n";
			s += "Header type: " + headerTypeFlag;
			return s;
		}
	}
}
