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
 * Revision 1.3  2005/02/08 12:54:40  kikidonk
 * Added cvs log and header
 *
 */

namespace Entagged.Audioformats.Flac.Util {
	public class MetadataBlockDataStreamInfo {
		
		private int samplingRate,length,bitsPerSample,channelNumber;
		private bool isValid = true;
		
		public MetadataBlockDataStreamInfo(byte[] b) {
			if(b.Length<19) {
				isValid = false;
				return;
			}
			
			samplingRate = ReadSamplingRate(b[10], b[11], b[12] );
			
			channelNumber = ((u(b[12])&0x0E)>>1) + 1;
			samplingRate = samplingRate / channelNumber;
			
			bitsPerSample = ((u(b[12])&0x01)<<4) + ((u(b[13])&0xF0)>>4) + 1;
			
			int sampleNumber = ReadSampleNumber(b[13], b[14], b[15], b[16], b[17]);
			
			length = sampleNumber / samplingRate;
		}
		
		public int Length {
			get { return length; }
		}
		
		public int ChannelNumber {
			get { return channelNumber; }
		}
		
		public int SamplingRate {
			get { return samplingRate; }
		}
		
		public string EncodingType {
			get { return "FLAC "+bitsPerSample+" bits"; }
		}
		
		public bool Valid {
			get { return isValid; }
		}
		

		private int ReadSamplingRate(byte b1, byte b2, byte b3) {
			int rate = (u(b3)&0xF0)>>3;
			rate += u(b2)<<5;
			rate += u(b1)<<13;
			return rate;
		}
		
		private int ReadSampleNumber(byte b1, byte b2, byte b3, byte b4, byte b5) {
			int nb = u(b5);
			nb += u(b4)<<8;
			nb += u(b3)<<16;
			nb += u(b2)<<24;
			nb += (u(b1)&0x0F)<<32;
			return nb;
		}
		
		private int u(int i) {
			return i & 0xFF;
		}
	}
}
