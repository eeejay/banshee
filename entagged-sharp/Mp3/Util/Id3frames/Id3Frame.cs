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
 * Revision 1.3  2005/10/31 19:13:03  abock
 * Updated entagged-sharp snapshot
 *
 * Revision 1.3  2005/02/08 12:54:40  kikidonk
 * Added cvs log and header
 *
 */

using System;
using System.Text;
using Entagged.Audioformats.Util;
using Entagged.Audioformats.Mp3;

namespace Entagged.Audioformats.Mp3.Util.Id3Frames {
	public abstract class Id3Frame {
		protected byte[] flags;
		protected byte version;
		
		public Id3Frame() {
			this.version = Id3Tag.ID3V23;
			CreateDefaultFlags();
		}
		
		public Id3Frame(byte[] raw, byte version)
		{
		    byte[] rawNew;
			if(version == Id3Tag.ID3V23) {
				byte size = 2;
				
				if((raw[1]&0x80) == 0x80) {
					//Compression zlib, 4 bytes uncompressed size.
					size += 4;
				}
				
				if((raw[1]&0x80) == 0x40) {
					//Encryption method byte
					size += 1;
				}
				
				if((raw[1]&0x80) == 0x20) {
					//Group identity byte
					size += 1;
				}
				
				this.flags = new byte[size];
				for(int i = 0; i<size; i++)
					this.flags[i] = raw[i];
				rawNew = raw;
			} else {
				CreateDefaultFlags();
				rawNew = new byte[this.flags.Length + raw.Length];
				Copy(this.flags, rawNew, 0);
				Copy(raw, rawNew, this.flags.Length);
			}
			
			this.version = version;
			
			Populate(rawNew);
		}
		
		private void CreateDefaultFlags() {
			this.flags = new byte[2];
			this.flags[0] = 0;
			this.flags[1] = 0;
		}
		
		public byte[] Flags {
			get { return this.flags; }
		}
		
		public byte[] RawContent {
			get { return Build(); }
		}
		
		
		public abstract bool IsBinary { 
			set;
			get;
		}
		public abstract bool IsEmpty { 
			get;
		}
		
		public abstract string Id {
			get;
		}
		protected abstract void Populate(byte[] raw);
		protected abstract byte[] Build();
		public abstract bool IsCommon {
			get;
		}
		public abstract void CopyContent(TagField field);
		
		protected int IndexOfFirstNull(byte[] b, int offset) {
			for(int i = offset; i<b.Length; i++)
				if(b[i] == 0)
					return i;
			return -1;
		}
		
		protected void Copy(byte[] src, byte[] dst, int dstOffset) {
			for(int i = 0; i<src.Length; i++)
				dst[i+dstOffset] = src[i];
		}
		
		protected byte[] IdBytes {
			get { return Encoding.GetEncoding("ISO-8859-1").GetBytes(Id); }
		}

		protected byte[] GetSize(int size) {
			byte[] b = new byte[4];
			b[0] = (byte)( ( size >> 24 ) & 0xFF );
			b[1] = (byte)( ( size >> 16 ) & 0xFF );
			b[2] = (byte)( ( size >>  8 ) & 0xFF );
			b[3] = (byte)(   size         & 0xFF );
			return b;
		}
		
		protected String GetString(byte[] b, int offset, int length, string encoding) {
			string result = null;
			if (encoding == "UTF-16") {
				int zerochars = 0;
				// do we have zero terminating chars (old entagged did not)
				if (b[offset+length-2] == 0x00 && b[offset+length-1] == 0x00) {
					zerochars = 2;
				}
				if (b[offset] == (byte) 0xFE && b[offset + 1] == (byte) 0xFF) {
					result = Encoding.GetEncoding("UTF-16BE").GetString(b, offset + 2, length - 2 - zerochars);
				} else if (b[offset] == (byte) 0xFF && b[offset + 1] == (byte) 0xFE) {
					result = Encoding.GetEncoding("UTF-16LE").GetString(b, offset + 2, length - 2 - zerochars);
				} 
			} else {
				if (length == 0 || offset+length > b.Length) {
					result = "";
				} else {
					int zerochars = 0;
					if (b[offset+length-1] == 0x00) {
						zerochars = 1;
					}
					result = Encoding.GetEncoding(encoding).GetString(b, offset, length-zerochars);
				}
			}
			return result;
		}
		
		protected byte[] GetBytes(string s, string encoding) {
			byte[] result = null;
			if (encoding == "UTF-16") {
				result = System.Text.Encoding.GetEncoding("UTF-16LE").GetBytes(s);
				// 2 for BOM and 2 for terminal character
				byte[] tmp = new byte[result.Length + 4];
				Copy(result, tmp, 2);
				// Create the BOM
				tmp[0] = (byte) 0xFF;
				tmp[1] = (byte) 0xFE;
				result = tmp;
			} else {
				// this is encoding ISO-8859-1, for the time of this change.
				result = System.Text.Encoding.GetEncoding(encoding).GetBytes(s);
				byte[] tmp = new byte[result.Length + 1];
				Copy(result, tmp, 0);
				result = tmp;
			}
			return result;
		}
	}
}
