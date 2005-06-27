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

using Entagged.Audioformats.Generic;

namespace Entagged.Audioformats.Ape.Util {
	public class MonkeyHeader {
		
		byte[] b;
		public MonkeyHeader(byte[] b) {
			this.b = b;
		}
		
		public int CompressionLevel {
			get { return Utils.GetNumber(b, 0, 1); }
		}
		
		public int FormatFlags {
			get { return Utils.GetNumber(b, 2,3); }
		}
		
		public long BlocksPerFrame {
			get { return Utils.GetLongNumber(b, 4,7); }
		}
		
		public long FinalFrameBlocks {
			get { return Utils.GetLongNumber(b, 8,11); }
		}
		
		public long TotalFrames {
			get { return Utils.GetLongNumber(b, 12,15); }
		}
	    
		public int Length {
			get { return (int) (BlocksPerFrame * (TotalFrames - 1.0) + FinalFrameBlocks) / SamplingRate; }
		}

		public int BitsPerSample {
			get { return Utils.GetNumber(b, 16,17); }
		}
		
		public int ChannelNumber {
			get { return Utils.GetNumber(b, 18,19); }
		}
		
		public int SamplingRate {
			get { return Utils.GetNumber(b, 20,23); }
			
		}
	}
}
