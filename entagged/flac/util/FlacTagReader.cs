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
using Entagged.Audioformats.Ogg.Util;
using Entagged.Audioformats.Ogg;

namespace Entagged.Audioformats.Flac.Util {
	public class FlacTagReader {
		
		private OggTagReader oggTagReader = new OggTagReader();
		
		public OggTag Read( Stream raf ) {
			//Begins tag parsing-------------------------------------
			if ( raf.Length==0 ) {
				//Empty File
				throw new CannotReadException("Error: File empty");
			}
			raf.Seek( 0 , SeekOrigin.Begin);

			//FLAC Header string
			byte[] b = new byte[4];
			raf.Read(b, 0, b.Length);
			string flac = new string(System.Text.Encoding.ASCII.GetChars(b));
			if(flac != "fLaC")
				throw new CannotReadException("fLaC Header not found, not a flac file");
			
			OggTag tag = null;
			
			//Seems like we hava a valid stream
			bool isLastBlock = false;
			while(!isLastBlock) {
				b = new byte[4];
				raf.Read(b, 0, b.Length);
				MetadataBlockHeader mbh = new MetadataBlockHeader(b);
			
				switch(mbh.BlockType) {
					//We got a vorbis comment block, parse it
					case MetadataBlockHeader.VORBIS_COMMENT : 	tag = HandleVorbisComment(mbh, raf);
																mbh = null;
																return tag; //We have it, so no need to go further
					
					//This is not a vorbis comment block, we skip to next block
					default : 	raf.Seek(raf.Position+mbh.DataLength, SeekOrigin.Begin);
								break;
				}

				isLastBlock = mbh.IsLastBlock;
				mbh = null;
			}
			//FLAC not found...
			throw new CannotReadException("FLAC Tag could not be found or read..");
		}
		
		private OggTag HandleVorbisComment(MetadataBlockHeader mbh, Stream raf) {
			long oldPos = raf.Position;
			
			OggTag tag = oggTagReader.Read(raf);
			
			long newPos = raf.Position;
			
			if(newPos - oldPos != mbh.DataLength)
				throw new CannotReadException("Tag length do not match with flac comment data length");
			
			return tag;
		}
	}
}
