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
	public class MonkeyDescriptor {
		
		byte[] b;
		public MonkeyDescriptor(byte[] b) {
			this.b = b;
		}
		
		public int RiffWavOffset {
			get { return DescriptorLength + HeaderLength + SeekTableLength; }
		}
		
		public int DescriptorLength {
			get { return Utils.GetNumber(b, 0,3); }
		}
		
		public int HeaderLength {
			get { return Utils.GetNumber(b, 4,7); }
		}
		
		public int SeekTableLength {
			get { return Utils.GetNumber(b, 8,11); }
		}
		
		public int RiffWavLength {
			get { return Utils.GetNumber(b, 12,15); }
		}
	    
		public long ApeFrameDataLength {
			get { return Utils.GetLongNumber(b, 16,19); }
		}
		
		public long ApeFrameDataHighLength {
			get { return Utils.GetLongNumber(b, 20,23); }
		}
		
		public int TerminatingDataLength {
			get { return Utils.GetNumber(b, 24,27); }
		}
	    
	    //16 bytes cFileMD5 b[28->43]
	}
}
