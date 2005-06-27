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
 * Revision 1.5  2005/02/18 12:31:51  kikidonk
 * Adds a way to know if there was an id3 tag or not
 *
 * Revision 1.4  2005/02/08 12:54:42  kikidonk
 * Added cvs log and header
 *
 */

using System;
using System.IO;
using Entagged.Audioformats.Generic;
using Entagged.Audioformats.exceptions;
using Entagged.Audioformats.Mp3.Util;

namespace Entagged.Audioformats.Mp3 {
	public class Mp3FileReader : AudioFileReader {
		
		private Mp3InfoReader ir = new Mp3InfoReader();
		private Id3v2TagReader idv2tr = new Id3v2TagReader();
		private Id3v1TagReader idv1tr = new Id3v1TagReader();
		
		protected override EncodingInfo GetEncodingInfo( Stream raf ) {
			return ir.Read(raf);
		}
		
		protected override Tag GetTag( Stream raf )  {
			string error = "";
			Id3v2Tag v2 = null;
			Id3v1Tag v1 = null;
			
			try {
				v2 = idv2tr.Read(raf);
			} catch(CannotReadException e) {
				v2 = null;
				error += "("+e.Message+")";
			}
			
			try {
				v1 = idv1tr.Read(raf);
			} catch(CannotReadException e) {
				v1 = null;
				error += "("+e.Message+")";
			}

			if(v1 == null && v2 == null)
				throw new CannotReadException("There is no id3 (v1 or v2) tag in this file: "+error);
			
			if(v2 == null) {
				return v1;
			}
			else if(v1 != null) {
				v2.Merge( v1 );
				v2.HasId3v1 = true;
			}
			return v2;
		}
	}
}
