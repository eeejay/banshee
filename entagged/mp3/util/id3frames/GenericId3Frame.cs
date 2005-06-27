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
 * Revision 1.1  2005/06/27 00:54:43  abock
 * entagged id3frames
 *
 * Revision 1.3  2005/02/08 12:54:40  kikidonk
 * Added cvs log and header
 *
 */

using System;
using Entagged.Audioformats.Generic;
using Entagged.Audioformats.Mp3;

namespace Entagged.Audioformats.Mp3.Util.Id3Frames {
	public class GenericId3Frame : Id3Frame {
		
		private byte[] data;
		private string id;
		
		/*
		 * 0,1| frame flags
		 * 2,..,0X00| Owner ID
		 * xx,...| identifier (binary)
		 */
		
		public GenericId3Frame(string id, byte[] raw, byte version) : base(raw, version) {
			this.id = id;
		}
		
		public override string Id {
			get { return this.id; }
		}
		
		public override bool IsBinary {
			get { return true; }
			set { /* NA */ }
		}
		
		public byte[] Data {
			get { return data; }
		}
		
		public override bool IsCommon {
			get { return false; }
		}
		
		public override void CopyContent(TagField field) {
		    if(field is GenericId3Frame)
		        this.data = (field as GenericId3Frame).Data;
		}
		
		public override bool IsEmpty {
		    get { return this.data.Length == 0; }
		}
		
		protected override void Populate(byte[] raw) {
			this.data = new byte[raw.Length - flags.Length];
			for(int i = 0; i<data.Length; i++)
				data[i] = raw[i + flags.Length];
		}
		
		protected override byte[] Build() {
			byte[] b = new byte[4 + 4 + data.Length + flags.Length];
			
			int offset = 0;
			Copy(IdBytes, b, offset);        offset += 4;
			Copy(GetSize(b.Length-10), b, offset); offset += 4;
			Copy(flags, b, offset);               offset += flags.Length;
			
			Copy(data, b, offset);
			
			return b;
		}
		
		public override string ToString() {
			return this.id+" : No associated view";
		}
	}
}
