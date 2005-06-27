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
	public abstract class Id3Frame : TagField {
		protected byte[] flags;
		protected byte version;
		
		public Id3Frame() {
			this.version = Id3v2Tag.ID3V23;
			CreateDefaultFlags();
		}
		
		public Id3Frame(byte[] raw, byte version) {
		    byte[] rawNew;
			if(version == Id3v2Tag.ID3V23) {
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
			get { return System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(Id); }
		}
		
		protected byte[] GetSize(int size) {
			byte[] b = new byte[4];
			b[0] = (byte)( ( size >> 24 ) & 0xFF );
			b[1] = (byte)( ( size >> 16 ) & 0xFF );
			b[2] = (byte)( ( size >>  8 ) & 0xFF );
			b[3] = (byte)(   size         & 0xFF );
			return b;
		}
		
		protected string GetString(byte[]b, int offset, int length, string encoding) {
			return new string(System.Text.Encoding.GetEncoding(encoding).GetChars(b, offset, length));
		}
		
		protected byte[] GetBytes(string s, string encoding) {
			return System.Text.Encoding.GetEncoding(encoding).GetBytes(s);
		}
	}
}
