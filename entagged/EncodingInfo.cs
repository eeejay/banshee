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
 * Revision 1.4  2005/02/18 13:38:12  kikidonk
 * Adds a isVbr method that checks wether the file is vbr or not, added check in OGG and MP3, other formats are always VBR
 *
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.Collections;
using System.Text;

namespace Entagged.Audioformats {

public class EncodingInfo {
		
	private Hashtable content;
	
	public EncodingInfo() {
		content = new Hashtable();
		content["BITRATE"] =  -1;
		content["CHANNB"] =  -1;
		content["TYPE"] =  "";
		content["INFOS"] =  "";
		content["SAMPLING"] =  -1;
		content["LENGTH"] = -1;
		content["VBR"] = true;
	}
	
	//Sets the bitrate in KByte/s
	public int Bitrate {
		set { content["BITRATE"] = value; }
		get { return (int) content["BITRATE"]; }
	}
	//Sets the number of channels
	public int ChannelNumber {
		set { content["CHANNB"] = value; }
		get { return (int) content["CHANNB"]; }
	}
	//Sets the type of the encoding, this is a bit format specific. eg:Layer I/II/II
	public string EncodingType {
		set { content["TYPE"] = value; }
		get { return (string) content["TYPE"]; }
	}
	//A string contianing anything else that might be interesting
	public string ExtraEncodingInfos {
		set { content["INFOS"] = value; }
		get { return (string) content["INFOS"]; }
	}
	//Sets the Sampling rate in Hz
	public int SamplingRate {
		set { content["SAMPLING"] = value; }
		get { return (int) content["SAMPLING"]; }
	}
	//Sets the length of the song in seconds
	public int Length {
		set { content["LENGTH"] = value; }
		get { return (int) content["LENGTH"]; }
	}
	
	public bool Vbr {
		set { content["VBR"] = value; }
		get { return (bool) content["VBR"]; }
	}
	
	
	//Pretty prints this encoding info
	public override string ToString() {
		StringBuilder sb = new StringBuilder();
		
		sb.Append("Encoding infos content:\n");
		foreach(DictionaryEntry entry in content) {
          sb.Append("\t");
		  sb.Append(entry.Key);
		  sb.Append(" : ");
		  sb.Append(entry.Value);
		  sb.Append("\n");
		}
		return sb.ToString().Substring(0,sb.Length-1);
	}
}
}
