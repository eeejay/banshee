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
	public class ApeTagTextField : ApeTagField, TagTextField  {
	    
	    private string content;

	    public ApeTagTextField(string id, string content) : base(id, false) {
	        this.content = content;
	    }
	    
	    public override bool IsEmpty {
	        get { return this.content == ""; }
	    }
	    
	    public override string ToString() {
	        return this.content;
	    }
	    
	    public override void CopyContent(TagField field) {
	        if(field is ApeTagTextField) {
	            this.content = (field as ApeTagTextField).Content;
	        }
	    }
	    
	    public string Content {
	        get { return this.content; }
	        set { this.content = value; }
	    }

	    public string Encoding {
	        get { return "UTF-8"; }
	        set { /* NA */ }
	    }

	    public override byte[] RawContent {
	        get {
		        byte[] idBytes = GetBytes(Id, "ISO-8859-1");
		        byte[] contentBytes = GetBytes(content, Encoding);
				byte[] buf = new byte[4 + 4 + idBytes.Length + 1 + contentBytes.Length];
				byte[] flags = {0x00,0x00,0x00,0x00};
				
				int offset = 0;
				Copy(GetSize(contentBytes.Length), buf, offset);offset += 4;
				Copy(flags, buf, offset);                       offset += 4;
				Copy(idBytes, buf, offset);                     offset += idBytes.Length;
				buf[offset] = 0;                                offset += 1;
				Copy(contentBytes, buf, offset);                offset += contentBytes.Length;
				
				return buf;
			}
	    }
	}
}
