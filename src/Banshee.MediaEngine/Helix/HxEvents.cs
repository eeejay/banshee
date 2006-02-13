
/***************************************************************************
 *  HxEvents.cs
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

namespace Helix
{
	public delegate void ErrorOccurredHandler(object o, ErrorOccurredArgs args);
	public delegate void LengthChangedHandler(object o, LengthChangedArgs args);
	public delegate void VolumeChangedHandler(object o, VolumeChangedArgs args);
	public delegate void ContentStateChangedHandler(object o,
		ContentStateChangedArgs args);
	public delegate void StatusChangedHandler(object o, StatusChangedArgs args);
	public delegate void ContentConcludedHandler(object o, HxPlayerArgs args);
	public delegate void MuteChangedHandler(object o, MuteChangedArgs args);
	public delegate void BufferingHandler(object o, BufferingArgs args);
	
	public class HxPlayerArgs : EventArgs
	{
		public HxPlayer Player;
	}
	
	public class ErrorOccurredArgs : HxPlayerArgs
	{
		public string Error;
		public string UserError;
		public string MoreInfoUrl;
	}
	
	public class LengthChangedArgs : HxPlayerArgs
	{
		public uint Length;
	}
	
	public class VolumeChangedArgs : HxPlayerArgs
	{
		public uint Volume;
	}
	
	public class ContentStateChangedArgs : HxPlayerArgs
	{
		public HxContentState OldState;
		public HxContentState NewState;
	}
	
	public class StatusChangedArgs : HxPlayerArgs
	{
		public string Status;
	}
	
	public class MuteChangedArgs : HxPlayerArgs
	{
		public bool Muted;
	}
	
	public class BufferingArgs : HxPlayerArgs
	{
		public HxBufferReason BufferingReason;
		public uint BufferingPercent;
	}
}
