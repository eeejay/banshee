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
using System.Text;
using Entagged.Audioformats.Generic;
using Entagged.Audioformats.Mp3;

namespace Entagged.Audioformats.Mp3.Util.Id3Frames {
	public class CommId3Frame : TextId3Frame, TagTextField {
		
		private string shortDesc;
		private string lang;
		
		/*
		 * 0,1| frame flags
		 * 2| encoding
		 * 3,4,5| lang
		 * 6,..,0x00(0x00)| short descr
		 * x,..| actual comment
		 */
		
		public CommId3Frame(string content) : base("COMM", content) {
			this.shortDesc = "";
			this.lang = "eng";
		}
		
		public CommId3Frame(byte[] rawContent, byte version) : base("COMM", rawContent, version) {}
		
		public string Langage {
			get { return this.lang; }
		}
		
		protected override void Populate(byte[] raw) {
			this.encoding = raw[flags.Length];
			
			this.lang = new string(System.Text.Encoding.GetEncoding("ISO-8859-1").GetChars(raw, flags.Length+1, 3));
			
			this.shortDesc = GetString(raw, flags.Length+4, raw.Length - flags.Length - 4, Encoding);
			
			string[] s = this.shortDesc.Split('\0');
			this.shortDesc = s[0];
			
			this.content = "";
			if(s.Length >= 2)
				this.content = s[1];		
		}
		
		protected override byte[] Build() {
			string text = this.shortDesc + "\0" + this.content;
			byte[] data = GetBytes(text, Encoding);
			byte[] lan = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(this.lang);
			
			//the return byte[]
			byte[] b = new byte[4 + 4 + flags.Length + 1 + 3 + data.Length];
			
			int offset = 0;
			Copy(IdBytes, b, offset);        offset += 4;
			Copy(GetSize(b.Length-10), b, offset); offset += 4;
			Copy(flags, b, offset);               offset += flags.Length;
			
			b[offset] = this.encoding;	offset += 1;
			
			Copy(lan, b, offset);		offset += lan.Length;
			
			Copy(data, b, offset);
			
			return b;
		}
		
		public string ShortDescription {
			get { return shortDesc; }
		}
		
		public override bool IsEmpty {
		    get { return this.content == "" && this.shortDesc == ""; }
		}
		
		public override void CopyContent(TagField field) {
		    base.CopyContent(field);
		    if(field is CommId3Frame) {
		        this.shortDesc = (field as CommId3Frame).ShortDescription;
		        this.lang = (field as CommId3Frame).Langage;
		    }
		}
		
		public override string ToString() {
			StringBuilder sb = new StringBuilder();
			sb.Append("[").Append(Langage).Append("] ").Append("(").Append(ShortDescription).Append(") ").Append(Content);
			return sb.ToString();
		}
	}
}
