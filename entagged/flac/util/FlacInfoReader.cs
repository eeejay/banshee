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
 * Revision 1.4  2005/02/08 12:54:40  kikidonk
 * Added cvs log and header
 *
 */

using System.IO;
using Entagged.Audioformats.exceptions;

namespace Entagged.Audioformats.Flac.Util {
	public class FlacInfoReader {
		public EncodingInfo Read(Stream raf) {
			//Read the infos--------------------------------------------------------
			if (raf.Length == 0) {
				//Empty File
				throw new CannotReadException("Error: File empty");
			}
			raf.Seek(0, SeekOrigin.Begin);

			//FLAC Header string
			byte[] b = new byte[4];
			raf.Read(b, 0, b.Length);
			string flac = new string(System.Text.Encoding.ASCII.GetChars(b));
			if (flac != "fLaC") {
				throw new CannotReadException("fLaC Header not found");
			}

			MetadataBlockDataStreamInfo mbdsi = null;
			bool isLastBlock = false;
			while (!isLastBlock) {
				b = new byte[4];
				raf.Read(b, 0, b.Length);
				MetadataBlockHeader mbh = new MetadataBlockHeader(b);

				if (mbh.BlockType == MetadataBlockHeader.STREAMINFO) {
					b = new byte[mbh.DataLength];
					raf.Read(b, 0, b.Length);

					mbdsi = new MetadataBlockDataStreamInfo(b);
					if (!mbdsi.Valid) {
						throw new CannotReadException("FLAC StreamInfo not valid");
					}
					break;
				}
				raf.Seek(raf.Position + mbh.DataLength, SeekOrigin.Begin);

				isLastBlock = mbh.IsLastBlock;
				mbh = null; //Free memory
			}

			EncodingInfo info = new EncodingInfo();
			info.Length = mbdsi.Length;
			info.ChannelNumber = mbdsi.ChannelNumber;
			info.SamplingRate = mbdsi.SamplingRate;
			info.EncodingType = mbdsi.EncodingType;
			info.ExtraEncodingInfos = "";
			info.Bitrate = ComputeBitrate(mbdsi.Length, raf.Length);

			return info;
		}

		private int ComputeBitrate(int length, long size) {
			return (int) ((size / 1000) * 8 / length);
		}
	}
}
