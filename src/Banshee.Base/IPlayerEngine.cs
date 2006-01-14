/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
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
 
/* 
	Banshee supports runtime-loadable player engines, such as a GStreamer
	or Helix player. A Player Engine is a class that implements the 
	Banshee.IPlayerEngine interface. The assembly containing this class
	and any necesary files (such as a shared library that the engine class
	P/Invokes into) must be installed in :
	
		/path/to/banshee/install/mediaengines/<config-name>.

	Banshee will detect the assembly with a class implementing the
	Banshee.IPlayerEngine interface.
	
	After detection, Banshee creates an instance of the implementing
	class. Because of this, the class SHOULD NOT perform any engine
	initialization routines in the constructor! Banshee needs an instance
	to read required fields which must contain information about the engine.
	
	The Initialize() method is used to perform all engine initialization,
	and the Dispose() method is used to perform any necessary engine
	cleanup. DO NOT USE the constructor/destructor for this purpose!
*/

using System;

using Banshee.Base;

namespace Banshee.MediaEngine
{	
	public interface IPlayerEngine
	{
		// Signals which must be implemented in order for a player engine
		// to behave properly with the Banshee UI.
		event  PlayerEngineErrorHandler Error;
		event  PlayerEngineVolumeChangedHandler VolumeChanged;
		event  PlayerEngineIterateHandler Iterate;
		event  EventHandler EndOfStream;
	
		void   Initialize();           // actual engine initialization
		void   TestInitialize();       // test Initialize and Finalize
		void   Dispose();              // engine finalization/shutdown
		
		bool   Open(TrackInfo track, Uri uri); // load a file into the engine
		void   Close();                // close an open file
		
		void   Play();                 // play a file
		void   Pause();                // pause a playing file

		bool   Loaded         { get; } // return true if file is loaded
		bool   Playing        { get; } // return true if file is playing
		
		ushort Volume         { get; set; } // volume 0-100
		uint   Position       { get; set; } // position 0-N (seconds)
		uint   Length         { get; } // Length of stream, 0 if unavailable
		
		TrackInfo Track       { get; } // TrackInfo for playing/loaded track

		string ConfigName     { get; } // GConf configuration name (gst/helix)
		string EngineName     { get; } // Short Display Name (GStreamer/Helix)
		string EngineLongName { get; } // Long Name (Helix hxclientkit Engine)
		string EngineDetails  { get; } // Describe the engine/framework
		string AuthorName     { get; } // Name of Engine Implementation Author
		string AuthorEmail    { get; } // Email address of above
		int    MajorVersion   { get; } // Major version of Engine
		int    MinorVersion   { get; } // Minor version of Engine
		
		string [] SupportedExtensions { get; }
	}
	
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
		public ushort Volume;
	}
	
	public class PlayerEngineIterateArgs : EventArgs
	{
		public uint Position;
	}
}
