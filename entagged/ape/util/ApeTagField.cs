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
