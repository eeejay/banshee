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
 * Revision 1.1  2005/06/27 00:47:25  abock
 * Added entagged-sharp
 *
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.IO;
using Entagged.Audioformats.Generic;
using Entagged.Audioformats.exceptions;

namespace Entagged.Audioformats.Mp3.Util {
	public class Id3v2TagReader {
		
		bool[] ID3Flags;
		Id3v2TagSynchronizer synchronizer = new Id3v2TagSynchronizer();
		
		Id3v23TagReader v23 = new Id3v23TagReader();
		
		public Id3v2Tag Read(Stream raf) {
			Id3v2Tag tag;

			byte[] b = new byte[3];

			raf.Read(b, 0, b.Length);
			raf.Seek(0, SeekOrigin.Begin);

			string ID3 = new string(System.Text.Encoding.ASCII.GetChars(b));

			if (ID3 != "ID3") {
				throw new CannotReadException("Not an ID3 tag");
			}
			//Begins tag parsing ---------------------------------------------
			raf.Seek(3, SeekOrigin.Begin);
			//----------------------------------------------------------------------------
			//Version du tag ID3v2.xx.xx
			string versionHigh=raf.ReadByte() +"";
			string versionID3 =versionHigh+ "." + raf.ReadByte();
			//------------------------------------------------------------------------- ---
			//D?tection de certains flags (A COMPLETER)
			this.ID3Flags = ProcessID3Flags( (byte) raf.ReadByte() );
			//----------------------------------------------------------------------------
			
	//			On extrait la taille du tag ID3
			int tagSize = ReadSyncsafeInteger(raf);
			//System.err.println("TagSize: "+tagSize);
			
	//			------------------NEWNEWNWENENEWNENWEWN-------------------------------
			//Fill a byte buffer, then process according to correct version
			b = new byte[tagSize+2];
			raf.Read(b, 0, b.Length);
			ByteBuffer bb = new ByteBuffer(b);
			
			if (ID3Flags[0]==true) {
			    //We have unsynchronization, first re-synchronize
			    bb = synchronizer.synchronize(bb);
			}
			
			if(versionHigh == "2") {
				tag = v23.Read(bb, ID3Flags, Id3v2Tag.ID3V22);
			}
			else if(versionHigh == "3") {
			    tag = v23.Read(bb, ID3Flags, Id3v2Tag.ID3V23);
			}
			else if(versionHigh == "4") {
				throw new CannotReadException("ID3v2 tag version "+ versionID3 + " not supported !");
			}
			else {
				throw new CannotReadException("ID3v2 tag version "+ versionID3 + " not supported !");
			}
			
			return tag;
		}
		
		private bool[] ProcessID3Flags(byte b) {
			bool[] flags;

			if (b != 0) {
				flags = new bool[4];

				int flag = b & 128;

				if (flag == 128)
					flags[0] = true;
				else
					flags[0] = false; //unsynchronisation
				flag = b & 64;
				if (flag == 64)
					flags[1] = true;
				else
					flags[1] = false; //Extended Header
				flag = b & 32;
				if (flag == 32)
					flags[2] = true;
				else
					flags[2] = false; //Experimental Indicator
				flag = b & 16;
				if (flag == 16)
					flags[3] = true;
				else
					flags[3] = false; //Footer Present
			} else {
				flags = new bool[4];
				flags[0] = false;
				flags[1] = false;
				flags[2] = false;
				flags[3] = false;
			}
			return flags;
		}
		
		
		private int ReadSyncsafeInteger(Stream raf)	{
			int value = 0;

			value += (raf.ReadByte()& 0xFF) << 21;
			value += (raf.ReadByte()& 0xFF) << 14;
			value += (raf.ReadByte()& 0xFF) << 7;
			value += raf.ReadByte()& 0xFF;

			return value;
		}
	}
}
