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

using Entagged.Audioformats.Generic;

namespace Entagged.Audioformats.Ogg.Util {
	public class OggTagField : TagTextField {

	    private bool common;

	    private string content;

	    private string id;

	    public OggTagField(byte[] raw) {
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
