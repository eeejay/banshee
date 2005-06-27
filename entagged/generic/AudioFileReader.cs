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
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System;
using System.IO;
using Entagged.Audioformats.exceptions;

namespace Entagged.Audioformats.Generic {
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
