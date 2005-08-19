/***************************************************************************
 *  Copyright 2005 Raphaël Slinckx <raphael@slinckx.net> 
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

/*
 * $Log$
 * Revision 1.5  2005/08/19 02:17:10  abock
 * Updated to entagged-sharp 0.1.4
 *
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using Entagged.Audioformats.Util;

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

	    public override byte[] RawContent
	    {
	        get {
		        byte[] idBytes = GetBytes(Id, "ISO-8859-1");
		        byte[] contentBytes = GetBytes(content, Encoding);
				byte[] buf = new byte[4 + 4 + idBytes.Length + 1 + contentBytes.Length];
				byte[] flags = {0x00,0x00,0x00,0x00};
				
				int offset = 0;
				Copy(GetSize(contentBytes.Length), buf, offset);
				offset += 4;
				
				Copy(flags, buf, offset);                       
				offset += 4;
				
				Copy(idBytes, buf, offset);                     
				offset += idBytes.Length;
				
				buf[offset] = 0;                                
				offset += 1;
				
				Copy(contentBytes, buf, offset);                
				offset += contentBytes.Length;
				
				return buf;
			}
	    }
	}
}
