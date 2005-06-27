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
 * Revision 1.4  2005/02/08 12:54:40  kikidonk
 * Added cvs log and header
 *
 */

using System.IO;
using Entagged.Audioformats.exceptions;

namespace Entagged.Audioformats.Mpc.Util {
	public class MpcInfoReader {
		public EncodingInfo Read( Stream raf ) {
			EncodingInfo info = new EncodingInfo();
			
			//Begin info fetch-------------------------------------------
			if ( raf.Length==0 ) {
				//Empty File
				throw new CannotReadException("File is empty");
			}
			raf.Seek( 0 , SeekOrigin.Begin);
		
			
			//MP+ Header string
			byte[] b = new byte[3];
			raf.Read(b, 0, b.Length);
			string mpc = new string(System.Text.Encoding.ASCII.GetChars(b));
			if (mpc != "MP+" && mpc == "ID3") {
				//TODO Do we have to do this ??
				//we have an ID3v2 tag at the beginning
				//We quickly jump to MPC data
				raf.Seek(6, SeekOrigin.Begin);
				int tagSize = ReadSyncsafeInteger(raf);
				raf.Seek(tagSize+10, SeekOrigin.Begin);
				
				//retry to read MPC stream
				b = new byte[3];
				raf.Read(b, 0, b.Length);
				mpc = new string(System.Text.Encoding.ASCII.GetChars(b));
				if (mpc != "MP+") {
					//We could definitely not go there
					throw new CannotReadException("MP+ Header not found");
				}
			} else if (mpc != "MP+"){
				throw new CannotReadException("MP+ Header not found");
			}
			
			b = new byte[25];
			raf.Read(b, 0, b.Length);
			MpcHeader mpcH = new MpcHeader(b);
			//We only support v7 Stream format, so if it isn't v7, then returned values
			//will be bogus, and the file will be ignored
			
			double pcm = mpcH.SamplesNumber;
			info.Length = (int) ( pcm * 1152 / mpcH.SamplingRate );
			info.ChannelNumber = mpcH.ChannelNumber;
			info.SamplingRate = mpcH.SamplingRate;
			info.EncodingType = mpcH.EncodingType;
			info.ExtraEncodingInfos = mpcH.EncoderInfo;
			info.Bitrate = ComputeBitrate( info.Length, raf.Length );

			return info;
		}
		
		private int ReadSyncsafeInteger(Stream raf)	{
			int value = 0;

			value += (raf.ReadByte()& 0xFF) << 21;
			value += (raf.ReadByte()& 0xFF) << 14;
			value += (raf.ReadByte()& 0xFF) << 7;
			value += raf.ReadByte() & 0xFF;

			return value;
		}

		private int ComputeBitrate( int length, long size ) {
			return (int) ( ( size / 1000 ) * 8 / length );
		}
	}
}
