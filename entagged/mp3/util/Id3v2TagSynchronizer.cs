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
 * Revision 1.5  2005/02/13 17:22:17  kikidonk
 * Support for APIC
 *
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using Entagged.Audioformats.Generic;

namespace Entagged.Audioformats.Mp3.Util {
	public class Id3v2TagSynchronizer {

	    public ByteBuffer synchronize(ByteBuffer b) {
	        ByteBuffer bb = new ByteBuffer(b.Capacity);
	        
	        while(b.Remaining >= 1) {
	        	byte cur = b.Get();
	            bb.Put(cur);
	            
	            if((cur&0xFF) == 0xFF && b.Remaining >=1 && b.Peek() == 0x00) { //First part of synchronization
	                b.Get();
	            }
	        }
	        
	        //We have finished filling the new bytebuffer, so set the limit, and rewind
	        bb.Limit = bb.Position;
	        bb.Rewind();
	        
	        return bb;
	    }

	}
}
