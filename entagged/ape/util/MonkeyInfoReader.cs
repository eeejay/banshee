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
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.IO;
using Entagged.Audioformats.Generic;
using Entagged.Audioformats.exceptions;

namespace Entagged.Audioformats.Ape.Util {
	public class MonkeyInfoReader {

		public EncodingInfo Read( Stream raf ) {
			EncodingInfo info = new EncodingInfo();
			
			//Begin info fetch-------------------------------------------
			if ( raf.Length == 0 ) {
				//Empty File
				throw new CannotReadException("File is empty");
			}
			raf.Seek( 0 , SeekOrigin.Begin);
		
			//MP+ Header string
			byte[] b = new byte[4];
			raf.Read(b, 0, b.Length);
			string mpc = new string(System.Text.Encoding.ASCII.GetChars(b));
			if (mpc != "MAC ") {
				throw new CannotReadException("'MAC ' Header not found");
			}
			
			b = new byte[4];
			raf.Read(b, 0, b.Length);
			int version = Utils.GetNumber(b, 0,3);
			if(version < 3970)
				throw new CannotReadException("Monkey Audio version <= 3.97 is not supported");
			
			b = new byte[44];
			raf.Read(b, 0, b.Length);
			MonkeyDescriptor md = new MonkeyDescriptor(b);
			
			b = new byte[24];
			raf.Read(b, 0, b.Length);
			MonkeyHeader mh = new MonkeyHeader(b);
			
			raf.Seek(md.RiffWavOffset, SeekOrigin.Begin);
			b = new byte[12];
			raf.Read(b, 0, b.Length);
			WavRIFFHeader wrh = new WavRIFFHeader(b);
			if(!wrh.Valid)
				throw new CannotReadException("No valid RIFF Header found");
			
			b = new byte[24];
			raf.Read(b, 0, b.Length);
			WavFormatHeader wfh = new WavFormatHeader(b);
			if(!wfh.Valid)
				throw new CannotReadException("No valid WAV Header found");
			
			info.Length = mh.Length;
			info.ChannelNumber = wfh.ChannelNumber ;
			info.SamplingRate = wfh.SamplingRate ;
			info.Bitrate = ComputeBitrate(info.Length, raf.Length) ;
			info.EncodingType = "Monkey Audio v" + (((double)version)/1000)+", compression level "+mh.CompressionLevel;
			info.ExtraEncodingInfos = "";
			
			return info;
		}
		
		private int ComputeBitrate( int length, long size ) {
			return (int) ( ( size / 1000 ) * 8 / length );
		}
	}
}
