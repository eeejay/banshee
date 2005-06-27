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
 * Revision 1.5  2005/02/18 13:38:11  kikidonk
 * Adds a isVbr method that checks wether the file is vbr or not, added check in OGG and MP3, other formats are always VBR
 *
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.IO;
using Entagged.Audioformats.exceptions;

namespace Entagged.Audioformats.Ogg.Util {
	public class OggInfoReader {
		public EncodingInfo Read( Stream raf )  {
			EncodingInfo info = new EncodingInfo();
			long oldPos = 0;
			
			//Reads the file encoding infos -----------------------------------
			raf.Seek( 0 , SeekOrigin.Begin);
			double PCMSamplesNumber = -1;
			raf.Seek( raf.Length-2, SeekOrigin.Begin);
			while(raf.Position >= 4) {
				if(raf.ReadByte()==0x53) {
					raf.Seek( raf.Position - 4, SeekOrigin.Begin);
					byte[] ogg = new byte[3];
					raf.Read(ogg, 0, 3);
					if(ogg[0]==0x4F && ogg[1]==0x67 && ogg[2]==0x67) {
						raf.Seek( raf.Position - 3, SeekOrigin.Begin);
						
						oldPos = raf.Position;
						raf.Seek(raf.Position + 26, SeekOrigin.Begin);
						int _pageSegments = raf.ReadByte()&0xFF; //Unsigned
						raf.Seek( oldPos , SeekOrigin.Begin);
						
						byte[] _b = new byte[27 + _pageSegments];
						raf.Read( _b, 0, _b.Length );

						OggPageHeader _pageHeader = new OggPageHeader( _b );
						raf.Seek(0, SeekOrigin.Begin);
						PCMSamplesNumber = _pageHeader.AbsoluteGranulePosition;
						break;
					}
				}	
				raf.Seek( raf.Position - 2, SeekOrigin.Begin);
			}
			
			if(PCMSamplesNumber == -1){
				throw new CannotReadException("Error: Could not find the Ogg Setup block");
			}
			

			//Supposing 1st page = codec infos
			//			2nd page = comment+decode info
			//...Extracting 1st page
			byte[] b = new byte[4];
			
			oldPos = raf.Position;
			raf.Seek(26, SeekOrigin.Begin);
			int pageSegments = raf.ReadByte()&0xFF; //Unsigned
			raf.Seek( oldPos , SeekOrigin.Begin);

			b = new byte[27 + pageSegments];
			raf.Read( b , 0,  b .Length);

			OggPageHeader pageHeader = new OggPageHeader( b );

			byte[] vorbisData = new byte[pageHeader.PageLength];

			raf.Read( vorbisData , 0,  vorbisData.Length);

			VorbisCodecHeader vorbisCodecHeader = new VorbisCodecHeader( vorbisData );

			//Populates encodingInfo----------------------------------------------------
			info.Length = (int) ( PCMSamplesNumber / vorbisCodecHeader.SamplingRate );
			info.ChannelNumber = vorbisCodecHeader.ChannelNumber;
			info.SamplingRate = vorbisCodecHeader.SamplingRate;
			info.EncodingType = vorbisCodecHeader.EncodingType;
			info.ExtraEncodingInfos = "";
			if(vorbisCodecHeader.NominalBitrate != 0
		        && vorbisCodecHeader.MaxBitrate == vorbisCodecHeader.NominalBitrate
		        && vorbisCodecHeader.MinBitrate == vorbisCodecHeader.NominalBitrate) {
			    //CBR
			    info.Bitrate = vorbisCodecHeader.NominalBitrate;
			    info.Vbr = false;
			}
			else if(vorbisCodecHeader.NominalBitrate != 0
			        && vorbisCodecHeader.MaxBitrate == 0
			        && vorbisCodecHeader.MinBitrate == 0) {
			    //Average vbr
			    info.Bitrate = vorbisCodecHeader.NominalBitrate;
			    info.Vbr = true;
			}
			else {
				info.Bitrate = ComputeBitrate( info.Length, raf.Length );
				info.Vbr = true;
			}

			return info;
		}

		private int ComputeBitrate( int length, long size ) {
			return (int) ( ( size / 1000 ) * 8 / length );
		}
	}
}
