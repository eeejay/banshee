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
 * Revision 1.3  2005/07/20 03:37:02  abock
 * Build system updates, hal-sharp
 *
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using Entagged.Audioformats.Util;

namespace Entagged.Audioformats.Ape.Util {
	public abstract class ApeTagField : TagField {
	    
	    private string id;
	    private bool binary;
	    
	    public ApeTagField(string id, bool binary) {
	        this.id = id;
	        this.binary = binary;
	    }
	    
	    public string Id {
	        get { return this.id; }
	    }

	    public bool IsBinary {
	        get { return binary; }
	        set { this.binary = value; }
	    }

	    public bool IsCommon {
	        get {
	        	return id == "Title" ||
				  id == "Album" ||
				  id == "Artist" ||
				  id == "Genre" ||
				  id == "Track" ||
				  id == "Year" ||
				  id == "Comment";
			}
	    }

	    protected void Copy(byte[] src, byte[] dst, int dstOffset) {
			for(int i = 0; i<src.Length; i++)
				dst[i+dstOffset] = src[i];
		}
		
		protected byte[] GetSize(int size) {
			byte[] b = new byte[4];
			b[3] = (byte) ( ( size & 0xFF000000 ) >> 24 );
			b[2] = (byte) ( ( size & 0x00FF0000 ) >> 16 );
			b[1] = (byte) ( ( size & 0x0000FF00 ) >> 8 );
			b[0] = (byte) (   size & 0x000000FF );
			return b;
		}
		
		protected byte[] GetBytes(string s, string encoding) {
			return System.Text.Encoding.GetEncoding(encoding).GetBytes(s);
		}
		
	    public abstract bool IsEmpty {
	    	get;
	    }
	    public abstract void CopyContent(TagField field);
	    public abstract byte[] RawContent {
	    	get;
	    }
	}
}
