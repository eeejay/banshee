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
 * Revision 1.5  2005/02/13 17:30:15  kikidonk
 * Little fix, peek should not forward the ponter
 *
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

namespace Entagged.Audioformats.Generic {
	public class ByteBuffer {
		
		private byte[] buf;
		private int pointer;
		
		public ByteBuffer(int capacity) {
			this.buf = new byte[capacity];
			this.pointer = 0;
		}
		
		public ByteBuffer(byte[] data) {
			this.buf = data;
			this.pointer = 0;
		}
		
		public int Capacity {
			get { return buf.Length; }
		}
		
		public int Position {
			get { return pointer; }
			set { this.pointer = value; }
		}
		
		public int Limit {
			get { return buf.Length; }
			set {
				byte[] newbuf = new byte[value+1];
				for(int i = 0; i<newbuf.Length; i++)
					newbuf[i] = buf[i];
				this.buf = newbuf;
			}
		}
		
		public int Remaining {
			get { return buf.Length - pointer; }
		}
		
		public byte Get() {
			return buf[pointer++];
		}
		
		public byte Peek() {
			return buf[pointer];
		}
		
		public void Get(byte[] data) {
			for(int i = 0; i<data.Length; i++)
				data[i] = buf[pointer++];
		}
		
		public void Put(byte b) {
			buf[pointer++] = b;
		}
		
		public void Rewind() {
			this.pointer = 0;
		}
	}
}
