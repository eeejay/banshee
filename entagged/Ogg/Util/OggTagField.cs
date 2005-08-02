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
 * Revision 1.4  2005/08/02 05:24:59  abock
 * Sonance 0.8 Updates, Too Numerous, see ChangeLog
 *
 * Revision 1.3  2005/02/08 12:54:40  kikidonk
 * Added cvs log and header
 *
 */

using Entagged.Audioformats.Util;

namespace Entagged.Audioformats.Ogg.Util {
	public class OggTagField : TagTextField {

	    private bool common;

	    private string content;

	    private string id;

	    public OggTagField(byte[] raw)
	    {
	        string field = new string(System.Text.Encoding.UTF8.GetChars(raw));
	        string[] splitField = field.Split('=');
	        if (splitField.Length > 1) {
	            this.id = splitField[0].ToUpper();
	            this.content = splitField[1];
	        } else {
	            //Either we have "XXXXXXX" without "="
	            //Or we have "XXXXXX=" with nothing after the "="
	            int i = field.IndexOf("="); 
	            if(i != -1) {
	                this.id = field.Substring(0, i+1);
	                this.content = "";
	            }
	            else {
		            //Beware that ogg ID, must be capitalized and contain no space..
		            this.id = "ERRONEOUS";
		            this.content = field;
	            }
	        }

	        CheckCommon();
	    }

	    public OggTagField(string fieldId, string fieldContent) {
	        this.id = fieldId.ToUpper();
	        this.content = fieldContent;
	        CheckCommon();
	    }

	    private void CheckCommon() {
	        this.common = id == "TITLE" || id == "ALBUM"
	                || id == "ARTIST" || id == "GENRE"
	                || id == "TRACKNUMBER" || id == "DATE"
	                || id == "DESCRIPTION" || id == "COMMENT"
	                || id == "TRACK";
	    }

	    protected void Copy(byte[] src, byte[] dst, int dstOffset) {
	        for (int i = 0; i < src.Length; i++)
	        	dst[i + dstOffset] = src[i];
	    }

	    public void CopyContent(TagField field) {
	        if (field is TagTextField)
	            this.content = (field as TagTextField).Content;
	    }

	    protected byte[] GetBytes(string s, string encoding) {
	        return System.Text.Encoding.GetEncoding(encoding).GetBytes(s);
	    }

	    public string Content {
	        get { return content; }
	        set { this.content = value; }
	    }

	    public string Encoding {
	        get { return "UTF-8"; }
	        set { /* NA */ }
	    }

	    public string Id {
	        get { return this.id; }
	    }

	    public byte[] RawContent {
	        get {
		        byte[] size = new byte[4];
		        byte[] idBytes = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(this.id);
		        byte[] contentBytes = GetBytes(this.content, "UTF-8");
		        byte[] b = new byte[4 + idBytes.Length + 1 + contentBytes.Length];

		        int length = idBytes.Length + 1 + contentBytes.Length;
		        size[3] = (byte) ((length & 0xFF000000) >> 24);
		        size[2] = (byte) ((length & 0x00FF0000) >> 16);
		        size[1] = (byte) ((length & 0x0000FF00) >> 8);
		        size[0] = (byte) (length & 0x000000FF);

		        int offset = 0;
		        Copy(size, b, offset);
		        offset += 4;
		        Copy(idBytes, b, offset);
		        offset += idBytes.Length;
		        b[offset] = (byte) 0x3D;
		        offset++;// "="
		        Copy(contentBytes, b, offset);

		        return b;
			}
	    }

	    public bool IsBinary {
	        get { return false; }
	        set { /* NA */ }
	    }

	    public bool IsCommon {
	        get { return common; }
	    }

	    public bool IsEmpty {
	        get { return this.content == ""; }
	    }
	    
	    public override string ToString() {
	    	return Content;
	    }
	}
}
