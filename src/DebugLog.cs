/***************************************************************************
 *  DebugLog.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
 
using System;
using System.IO;
using System.Diagnostics;

namespace Banshee
{
	public class DebugLog
	{
		private static int callNumber = 0;
	
		public static void Add(string message, params object[] args)
		{
			/*try {
				StackFrame sf = new StackFrame(1, true);
				//string methodName = sf.GetMethod().ToString();
				string fileName = sf.GetFileName().ToString();
				int lineNumber = sf.GetFileLineNumber();
				
				Console.WriteLine("{0}{1} {2}",
					String.Format("{0}: {1}", callNumber++, 
						Path.GetFileName(fileName)).PadRight(18), 
					String.Format("[{0}]", lineNumber++).PadRight(6),
					String.Format(message, args));
			} catch(Exception) {*/
				Console.WriteLine("{0}: {1}", callNumber++, 
					String.Format(message, args));
			//}
		}
	}
}
