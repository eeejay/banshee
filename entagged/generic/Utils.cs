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
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.Text;

namespace Entagged.Audioformats.Generic {
	public class Utils {
		private static Encoding utf = Encoding.UTF8;
		public static string GetExtension(string f) {
			string name = f.ToLower ();
			int i = name.LastIndexOf( "." );
			if(i == -1)
				return "";
			
			return name.Substring( i + 1 );
		}
		
		public static byte[] GetUTF8Bytes(string s) {
			return utf.GetBytes(s);
		}
		
		public static long GetLongNumber(byte[] b, int start, int end) {
			long number = 0;
			for(int i = 0; i<(end-start+1); i++) {
				number += ((b[start+i]&0xFF) << i*8);
			}
			
			return number;
		}
		
		public static int GetNumber( byte[] b, int start, int end) {
			int number = 0;
			for(int i = 0; i<(end-start+1); i++) {
				number += ((b[start+i]&0xFF) << i*8);
			}
			
			return number;
		}
	}
}
