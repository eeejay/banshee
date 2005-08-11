/***************************************************************************
 *  GstPlayerEngine.cs
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

namespace Banshee
{	
	internal delegate void GpeErrorCallback(IntPtr engine, IntPtr error);
	internal delegate void GpeIterateCallback(IntPtr engine, int position,
		int length);
	internal delegate void GpeEndOfStreamCallback(IntPtr engine);

	public class GstPlayer : IPlayerEngine
	{
		[DllImport("libgstmediaengine")]
		private static extern IntPtr gpe_new();
		
		[DllImport("libgstmediaengine")]
		private static extern void gpe_free(HandleRef handle);
		
		/*[DllImport("libgstmediaengine")]
		private static extern void gpe_set_end_of_stream_handler(
			HandleRef handle, GpeEndOfStreamCallback cb);
			
		[DllImport("libgstmediaengine")]
		private static extern void gpe_set_error_handler(
			HandleRef handle, GpeErrorCallback cb);
			
		[DllImport("libgstmediaengine")]
		private static extern void gpe_set_iterate_handler(
			HandleRef handle, GpeIterateCallback cb);*/

		[DllImport("libgstmediaengine")]
		private static extern bool gpe_open(HandleRef handle, string file);
		
		[DllImport("libgstmediaengine")]
		private static extern void gpe_play(HandleRef handle);
		
		[DllImport("libgstmediaengine")]
		private static extern void gpe_pause(HandleRef handle);
	
		[DllImport("libgstmediaengine")]
		private static extern void gpe_stop(HandleRef handle);
		
		[DllImport("libgstmediaengine")]
		private static extern void gpe_set_volume(HandleRef handle,
			int volume);
		
		[DllImport("libgstmediaengine")]
		private static extern int gpe_get_volume(HandleRef handle);
		
		[DllImport("libgstmediaengine")]
		private static extern void gpe_set_position(HandleRef handle,
			int position);
			
		[DllImport("libgstmediaengine")]
		private static extern int gpe_get_position(HandleRef handle);
		
		[DllImport("libgstmediaengine")]
		private static extern int gpe_get_length(HandleRef handle);
		
		[DllImport("libgstmediaengine")]
		private static extern bool gpe_is_eos(HandleRef handle);
		
		[DllImport("libgstmediaengine")]
		private static extern IntPtr gpe_get_error(HandleRef handle);
		
		[DllImport("libgstmediaengine")]
		private static extern bool gpe_have_error(HandleRef handle);
		
		public event PlayerEngineErrorHandler Error;
		public event PlayerEngineVolumeChangedHandler VolumeChanged;
		public event PlayerEngineIterateHandler Iterate;
		public event EventHandler EndOfStream;
		
		private HandleRef handle;
		private bool loaded;
		private bool playing;
		
		private bool finalized = false;
		
		private bool timeoutCancelRequest = false;
		
		public void Initialize()
		{
			IntPtr ptr = gpe_new();
			handle = new HandleRef(this, ptr);
			//gpe_set_end_of_stream_handler(handle, OnEndOfStream);
			//gpe_set_error_handler(handle, OnError);
			//gpe_set_iterate_handler(handle, OnIterate);
			
			GLib.Timeout.Add(250, OnTimeout);
		}
		
		public void TestInitialize()
		{

		}
			
		public void Dispose()
		{
			if(!finalized) {
				finalized = true;
				timeoutCancelRequest = true;
				Close();
				gpe_free(handle);
			}
		}
			
		public bool Open(ITrackInfo ti)
		{
			if(loaded || playing)
				Close();
			loaded = gpe_open(handle, "file://" + ti.Uri);
			return loaded;
		}
		
		public void Close()
		{
			gpe_stop(handle);
			loaded = false;
			playing = false;
		}
			
		public void Play()
		{
			gpe_play(handle);
			playing = true;
		}
        
        public void Pause()
        {
        	gpe_pause(handle);
        	playing = false;
    	}
    	   	
       	public bool Playing 
       	{ 
       		get {
       			return playing;
       		} 
       	}
       	
       	public bool HasFile 
       	{ 
       		get {
       			return loaded;
       		} 
       	}
       	
       	public bool Loaded
       	{
       		get {
       			return loaded;
       		}
       	}
       	
       	public uint Position 
       	{ 
       		get { 
       			return (uint)gpe_get_position(handle);
       		} 
       		
       		set {
       			gpe_set_position(handle, (int)value);			
       		}
       	}
       	
       	public uint Length
       	{
       		get {
       			return (uint)gpe_get_length(handle);
       		}
       	}
       	
	    public ushort Volume 
	    {
	        get { 
	        	return (ushort)gpe_get_volume(handle);
	        }
	        
	        set { 
				gpe_set_volume(handle, (int)value);
				
				if(VolumeChanged != null) {
					PlayerEngineVolumeChangedArgs args = 
						new PlayerEngineVolumeChangedArgs();
					args.Volume = value;
					EmitVolumeChanged(args);
				}
	        }
	    }

		private bool OnTimeout()
		{
			if(timeoutCancelRequest) 
				return false;
				
			if(!loaded)
				return true;
				
			if(gpe_have_error(handle)) {
				Close();
				IntPtr errorPtr = gpe_get_error(handle);
				PlayerEngineErrorArgs errargs = new PlayerEngineErrorArgs();
				errargs.Error = Marshal.PtrToStringAnsi(errorPtr);
				EmitError(errargs);
				EmitEndOfStream();
				playing = false;
				return true;
			}
			
			PlayerEngineIterateArgs iterargs = new PlayerEngineIterateArgs();
			iterargs.Position = Position;
			EmitIterate(iterargs);
			
			if(gpe_is_eos(handle) && playing) {
				playing = false;
				EmitEndOfStream();
			}
			
			return true;
		}

		/*private void OnEndOfStream(IntPtr engine)
		{
			EmitEndOfStream();
		}
		
		private void OnError(IntPtr engine, IntPtr messagePtr)
		{
			PlayerEngineErrorArgs args = new PlayerEngineErrorArgs();
			args.Error = Marshal.PtrToStringAnsi(messagePtr);
			EmitError(args);
		}
		
		private void OnIterate(IntPtr engine, int position, int total)
		{
			PlayerEngineIterateArgs args = new PlayerEngineIterateArgs();
			args.Position = position;
			EmitIterate(args);
		}*/
			
		private void EmitError(PlayerEngineErrorArgs args)
		{
			PlayerEngineErrorHandler handler = Error;
			if(handler != null)
				handler(this, args);
		}
			
		private void EmitVolumeChanged(PlayerEngineVolumeChangedArgs args)
		{
			PlayerEngineVolumeChangedHandler handler = VolumeChanged;
			if(handler != null)
				handler(this, args);
		} 
		
		private void EmitIterate(PlayerEngineIterateArgs args)
		{
			PlayerEngineIterateHandler handler = Iterate;
			if(handler != null)
				handler(this, args);
		}
		
		private void EmitEndOfStream()
		{
			EventHandler handler = EndOfStream;
			if(handler != null)
				handler(this, new EventArgs());
		}

		public string ConfigName     { get { return "gstreamer";           } }
		public string EngineName     { get { return "GStreamer";           } }
		public string EngineLongName { get { return "GStreamer Engine";    } }
		public int MajorVersion      { get { return 0;                     } }
		public int MinorVersion      { get { return 1;                     } }
		
		public string AuthorName     { get { return "Aaron Bockover";      } }
		public string AuthorEmail    { get { return "aaron@aaronbock.net"; } }
	
		public string EngineDetails
		{
			get {
				return "GStreamer is a multimedia framework for playing and " +
				"manipulating media. Any GStreamer plugin " + 
				"that is available will work through this engine.";
			}
		}	
	}
}
