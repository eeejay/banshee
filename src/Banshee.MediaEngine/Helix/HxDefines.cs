/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  HxDefines.cs
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
 
namespace Helix
{
	public enum HxContentState : uint {
		NotLoaded = 0,
		Contacting,
		Loading,
		Stopped,
		Playing,
		Paused
	};

	public enum HxBufferReason : uint {
		StartUp = 0,
		Seek,
		Congestion,
		LivePause
	};

	public enum HxEqBand : uint {
		Band31Hz = 0,
		Band62Hz = 1,
		Band125Hz = 2,
		Band250Hz = 3,
		Band500Hz = 4,
		Band1KHz = 5,
		Band2KHz = 6,
		Band4KHz = 7,
		Band8KHz = 8,
		Band16KHz = 9,
		Count
	};

	public enum HxVideoAttribute : uint {
		Brightness = 0,
		Contrast = 1,
		Saturation = 2,
		Hue = 3,
		Sharpness = 4
	};

	public enum HxStatisticValueType : uint {
		TypeInternalUse = 0,
		Type32BitSignedInt = 2,
		TypeString = 4
	};

	public enum HxAudioStreamType : uint {
		Streaming = 0,
		Instantaneous = 1,
		Timed = 2,
		StreamingInstantaneous = 3
	};
}
