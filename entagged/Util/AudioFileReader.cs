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
 * Revision 1.4  2005/08/02 05:24:59  abock
 * Sonance 0.8 Updates, Too Numerous, see ChangeLog
 *
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System;
using System.IO;
using Entagged.Audioformats.Exceptions;

namespace Entagged.Audioformats.Util {
	public abstract class AudioFileReader {
		
		protected abstract EncodingInfo GetEncodingInfo( Stream raf );
		protected abstract Tag GetTag( Stream raf );
		
		public AudioFile Read(string f) {
			FileInfo finfo = new FileInfo(f);
			
			if(finfo.Length <= 150)
				throw new CannotReadException("Less than 150 byte \""+f+"\"");
			
			Stream raf = null;
			try{
				raf = finfo.OpenRead ();
				raf.Seek( 0, SeekOrigin.Begin );
				
				EncodingInfo info = GetEncodingInfo(raf);
			
				Tag tag;
				try {
					raf.Seek( 0, SeekOrigin.Begin );
					tag = GetTag(raf);
				} catch (CannotReadException e) {
					tag = new GenericTag();
				}
			
				return new AudioFile(f, info, tag);
			} catch ( Exception e ) {
				throw new CannotReadException("\""+f+"\" :"+e);
			}
			finally {
				try{
					if(raf != null)
							raf.Close();
				}catch(Exception ex){
					/* We tried everything... */
				}
			}
		}
	}
}
