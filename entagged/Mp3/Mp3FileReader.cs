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
 * Revision 1.1  2005/06/30 16:56:23  abock
 * New entagged
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
using Entagged.Audioformats.Util;
using Entagged.Audioformats.Exceptions;
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
				return new GenericTag();
			
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
