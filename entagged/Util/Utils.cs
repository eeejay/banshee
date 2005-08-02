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
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.Text;

namespace Entagged.Audioformats.Util {
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
