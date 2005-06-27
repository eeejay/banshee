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
 * Revision 1.1  2005/06/27 00:47:22  abock
 * Added entagged-sharp
 *
 * Revision 1.4  2005/02/22 19:38:43  kikidonk
 * Adding missing encoding infos accessor methods
 *
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using Entagged.Audioformats.Generic;

namespace Entagged.Audioformats {

	public class AudioFile {
		
		private string s;
		private EncodingInfo info;
		private Tag tag;
		
		public AudioFile(string s, EncodingInfo info, Tag tag) {
			this.s = s;
			this.info = info;
			this.tag = tag;
		}
		
		public AudioFile(string s, EncodingInfo info) {
			this.s = s;
			this.info = info;
			this.tag = new GenericTag();
		}
		
		public int Bitrate {
			get { return info.Bitrate; }
		}

		public int ChannelNumber {
			get { return info.ChannelNumber; }
		}
		
		public string EncodingType {
			get { return info.EncodingType; }
		}

		public string ExtraEncodingInfos {
			get { return info.ExtraEncodingInfos; }
		}

		public int getSamplingRate {
			get { return info.SamplingRate; }
		}

		public int getLength {
			get { return info.Length; }
		}
		
		public bool IsVbr {
			get { return info.Vbr; }
		}
		
		public Tag Tag {
			get { return (tag == null) ? new GenericTag() : tag; }
		}
		
		public override string ToString() {
			return "AudioFile "+s+"  --------\n"+info.ToString()+"\n"+ ( (tag == null) ? "" : tag.ToString())+"\n-------------------";
		}
	}
}
