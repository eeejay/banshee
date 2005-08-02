/***************************************************************************
 *  Copyright 2005 Raphaël Slinckx <raphael@slinckx.net> 
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

/*
 * $Log$
 * Revision 1.6  2005/08/02 05:24:53  abock
 * Sonance 0.8 Updates, Too Numerous, see ChangeLog
 *
 * Revision 1.7  2005/02/25 15:37:40  kikidonk
 * Big structure change
 *
 * Revision 1.6  2005/02/25 15:31:16  kikidonk
 * Big structure change
 *
 * Revision 1.5  2005/02/13 17:23:21  kikidonk
 * Moving tag viewing to a dedicated class, that also show images in gtk# where pplicable
 *
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using Entagged.Audioformats.Ape;
using Entagged.Audioformats.Exceptions;
using Entagged.Audioformats.Flac;
using Entagged.Audioformats.Util;
using Entagged.Audioformats.Mp3;
using Entagged.Audioformats.Mpc;
using Entagged.Audioformats.Ogg;
using Entagged.Audioformats.M4a;
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
			readers["m4a"] = new M4aFileReader();
			readers["m4p"] = readers["m4a"];
		}
		
		public static AudioFile Read(string f) {
			string ext = Utils.GetExtension(f);
			
			object afr = readers[ext];
			if( afr == null)
				throw new CannotReadException("No Reader associated to this extension: "+ext);
			
			return (afr as AudioFileReader).Read(f);
		}
	}
}
