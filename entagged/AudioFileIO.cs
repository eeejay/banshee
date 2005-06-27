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
 * Revision 1.5  2005/02/13 17:23:21  kikidonk
 * Moving tag viewing to a dedicated class, that also show images in gtk# where pplicable
 *
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using Entagged.Audioformats.Ape;
using Entagged.Audioformats.exceptions;
using Entagged.Audioformats.Flac;
using Entagged.Audioformats.Generic;
using Entagged.Audioformats.Mp3;
using Entagged.Audioformats.Mpc;
using Entagged.Audioformats.Ogg;
using System.Collections;
using System.IO;
using System;

namespace Entagged.Audioformats {
	public class AudioFileIO {		
		//These tables contains all the readers/writers associated with extension as a key
		private static Hashtable readers = new Hashtable();
		
		//Initialize the different readers/writers
		static AudioFileIO() {
			//Tag Readers
			readers["mp3"] = new Mp3FileReader();
			readers["ogg"] = new OggFileReader();
			readers["flac"] = new FlacFileReader();
			readers["mpc"] = new MpcFileReader();
			readers["mp+"]= readers["mpc"];
			readers["ape"] = new MonkeyFileReader();
		}
		
		public static AudioFile Read(string f) {
			string ext = Utils.GetExtension(f);
			
			object afr = readers[ext];
			if( afr == null)
				throw new CannotReadException("No Reader associated to this extension: "+ext);
			
			return (afr as AudioFileReader).Read(f);
		}
		
		/*
		public static void Main(string[] args) {
			foreach(string file in args) {
				Console.WriteLine("Tag content: "+file);
				try {
					AudioFile af = AudioFileIO.Read(file);
					Console.WriteLine(af);						
				} catch(Exception e) {
					Console.WriteLine(e);
				}
				Console.WriteLine("------------------------------\n");
			}
		}
		*/
	}
}
