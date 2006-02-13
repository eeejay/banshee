
/***************************************************************************
 *  HxUnmanaged.cs
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
using System.Runtime.InteropServices;

namespace Helix
{
	public class HxUnmanaged
	{
		[DllImport("hxclient")]
		public static extern bool ClientPlayerCreate(out int token, 
			IntPtr pWindow, IntPtr userInfo, IntPtr callbackStruct);

		[DllImport("hxclient")]
		public static extern void ClientPlayerClose(int token);

		[DllImport("hxclient")]
		public static extern bool ClientPlayerOpenURL(int token,
			string url, string mimeType);

		[DllImport("hxclient")]
		public static extern void ClientEngineProcessXEvent(IntPtr window);
		
		[DllImport("hxclient")]
		public static extern void ClientPlayerPlay(int token);
		
		[DllImport("hxclient")]
		public static extern void ClientPlayerPause(int token);
		
		[DllImport("hxclient")]
		public static extern void ClientPlayerStop(int token);
		
		[DllImport("hxclient")]
		public static extern UInt32 ClientPlayerGetPosition(int token);
		
		[DllImport("hxclient")]
		public static extern void ClientPlayerSetPosition(int token, 
			UInt32 position);
			
		[DllImport("hxclient")]
		public static extern UInt32 ClientPlayerGetLength(int token);
		
		[DllImport("hxclient")]
		public static extern void ClientPlayerSetVolume(int token,
			UInt16 volume);	
			
		[DllImport("hxclient")]
		public static extern void ClientPlayerMute(int token,
			bool shouldMute);

		// COM Plugin Stuff

        [DllImport("hxclient")]
        public static extern bool ClientPlayerGetPluginHandler(int token, 
            out IntPtr pluginHandlerPtr);

	    [DllImport("hxclient")]
	    public static extern uint ClientPlayerGetPluginCount(IntPtr pluginHandlerPtr);
	    
	    [DllImport("hxclient")]
	    public static extern bool ClientPlayerGetPlugin(IntPtr pluginHandlerPtr, uint index, 
	       out IntPtr pluginPtr);
	       
	    [DllImport("hxclient")]
	    public static extern bool ClientPlayerGetPluginFileFormatInfo(IntPtr pluginPtr, 
	       out IntPtr fileMimeTypes, out IntPtr fileExtensions, out IntPtr fileOpenNames);
	}
}
