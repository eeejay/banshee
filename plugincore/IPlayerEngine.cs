/***************************************************************************
 *  IPlayerEngine.cs
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

namespace Sonance
{
	public delegate void PlayerEngineErrorHandler(object o, 
		PlayerEngineErrorArgs args);
	
	public delegate void PlayerEngineVolumeChangedHandler(object o, 
		PlayerEngineVolumeChangedArgs args);
	
	public delegate void PlayerEngineIterateHandler(object o, 
		PlayerEngineIterateArgs args);
	
	public class PlayerEngineErrorArgs : EventArgs
	{
		public string Error;
	}
	
	public class PlayerEngineVolumeChangedArgs : EventArgs
	{
		public double Volume;
	}
	
	public class PlayerEngineIterateArgs : EventArgs
	{
		public long Position;
	}
	
	public interface IPlayerEngine
	{
		bool Open(ITrackInfo track);
		void Play();
		void Pause();
		void Shutdown();
		
		string EngineName {
			get;
		}
		
		bool Loaded {
			get;
		}
		
		bool Playing {
			get;
		}
		
		double Volume {
			get;
			set;
		}
		
		long Position {
			get;
			set;
		}
	
		// --- //
		
		event PlayerEngineErrorHandler Error;
		event PlayerEngineVolumeChangedHandler VolumeChanged;
		event PlayerEngineIterateHandler Iterate;
		event EventHandler EndOfStream;
	}
}
