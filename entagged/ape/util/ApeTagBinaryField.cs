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
	public class ApeTagBinaryField : ApeTagField  {
	    
	    private byte[] content;

	    public ApeTagBinaryField(string id, byte[] content) : base(id, true) {
	        this.content = new byte[content.Length];
	        for(int i = 0; i<content.Length; i++)
	            this.content[i] = content[i];
	    }
	    
	    public override bool IsEmpty {
	        get { return this.content.Length == 0; }
	    }
	    
	    public override string ToString() {
	        return Id + " : Cannot represent this";
	    }
	    
	    public override void CopyContent(TagField field) {
	        if(field is ApeTagBinaryField) {
	            this.content = (field as ApeTagBinaryField).Content;
	        }
	    }
	    
	    public byte[] Content {
	        get { return this.content; }
	    }
	    
	    public override byte[] RawContent {
	        get {
		        byte[] idBytes = GetBytes(Id, "ISO-8859-1");
		        byte[] buf = new byte[4 + 4 + idBytes.Length + 1 + content.Length];
				byte[] flags = {0x02,0x00,0x00,0x00};
				
				int offset = 0;
				Copy(GetSize(content.Length), buf, offset);    offset += 4;
				Copy(flags, buf, offset);                      offset += 4;
				Copy(idBytes, buf, offset);                    offset += idBytes.Length;
				buf[offset] = 0;                               offset += 1;
				Copy(content, buf, offset);                    offset += content.Length;
				
				return buf;
			}
	    }
	}
}
