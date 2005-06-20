/***************************************************************************
 *  GstPlayer.cs
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
using System.Threading;
using Gst;
using GLib;
	
namespace Sonance
{	
	public class Engine
	{
		static public bool Initialize(string [] args)
		{
			Element dummy;
			
			Gst.Application.Init("Sonance", ref args);
			
			try {
				dummy = Gst.ElementFactory.Make("fakesink", "fakesink");
				Gst.SchedulerFactory.Make(null, dummy);
			} catch(Exception e) {
				new Error(
					"Could not initialize GStreamer scheduler. " +
					"Did you run gst-register?", e);
				DebugLog.Add("Failed to initialize GStreamer scheduler");
				return false;
			}
			
			DebugLog.Add("GStreamer is working");
			return true;
		}
	}
	
	public class TickEventArgs : EventArgs
	{
    	public long Position;
	}
	
	public class VolumeChangedEventArgs : EventArgs
	{
		public double Volume;
	}
	
	public delegate void TickEventHandler(object o, TickEventArgs args);
	public delegate void VolumeChangedEventHandler(object o, 
		VolumeChangedEventArgs args);
	
	public class GstPlayer
	{
		private Bin pipeline = null;
		
		private Element sink;
		private Element src;
		private Element decoder;
		private Element volume;
	
		private System.Threading.Thread thread;
		
		private ElementState requestedState = ElementState.Ready;
		
		private string uri = null;
		private bool playing = false;
		private long time;
		private double volumeValue = 0.8;
		
		private uint timeoutHandle = 0;
		private bool finalizing = false;
		private bool atEos = false;
		
		public event TickEventHandler Tick;
		public event EventHandler Eos;
		public event VolumeChangedEventHandler VolumeChanged;
		
		private bool Construct()
		{
			pipeline = new Pipeline("pipeline");
		
			src = ElementFactory.Make("gnomevfssrc", "gnomevfssrc");
			if(src == null) {
				new Error("Could not construct gnomevfssrc element");
				return false;
			}
			
			decoder = ElementFactory.Make("spider", "spider");
			if(decoder == null) {
				new Error("Could not construct a decoder element");
				return false;
			}
			
			volume = ElementFactory.Make("volume", "volume");
			if(volume == null) {
				new Error("Could not construct a volume element");
				return false;
			}
			
			volume.SetProperty("volume", volumeValue);
		
			sink = Gst.Gconf.DefaultAudioSink;
			if(sink == null) {
				new Error("Could not construct an output sink element");
				return false;
			}

			pipeline.Add(src);
			pipeline.Add(decoder);
			pipeline.Add(volume);
			pipeline.Add(sink);

			src.Link(decoder);
			decoder.Link(volume);
			volume.Link(sink);
			
			timeoutHandle = GLib.Timeout.Add(250, 
				new TimeoutHandler(OnTimeout));
			
			DebugLog.Add("GstPlayer: new pipeline constructed");
			
			return true;
		}
		
		public void Iterate() 
		{
			int format;
			pipeline.SetState(requestedState);
			ElementState currentPlayingState = requestedState;
			double lastVolumeValue = 0;
			
			DebugLog.Add("GstPlayer: pipeline iterate enter");
			
			try {
			
				while(pipeline.Iterate()) {
					if(volume != null && lastVolumeValue != volumeValue) {
						lastVolumeValue = volumeValue;
						volume.SetProperty("volume", lastVolumeValue);
					}

					long temp_time;
					format = (int)Format.Time;
					sink.Query(QueryType.Position, ref format, out temp_time);
					time = temp_time / 1000000000;

					if(requestedState != currentPlayingState) {
						pipeline.SetState(requestedState);
						currentPlayingState = requestedState;
					}
				}
				
				if(requestedState != ElementState.Paused && !finalizing) {
					DebugLog.Add("GstPlayer: pipeline iterate complete");
					Finalize(false);
					return;
				}
			
			} catch(Exception) {
				// Probably a DivisionByZero exception... very weird
				// I just stumbled across an MP3 of mine, 
				// "One Way Glass - In My Head"
				// It's in GStreamer core, and I can't do much about it
				// Even if this is caught, I think it causes GStreamer to 
				// actually crash, so we're fucked anyway
				
				// More info seems that if the date tag is 0 and it's
				// application/x-id3... it does this
				
				// GStreamer plays it when:
				// gst-launch gnomevfssrc location="One Way Glass - In My Head.mp3" ! spider ! alsasink
				// gst-launch gnomevfssrc location="One Way Glass - In My Head.mp3" ! spider ! volume volume=0.5 ! alsasink
				
				// I thought it might then be the time query above, so I tried
				// it with that commented out... still no luck. I'm lost.
				Finalize(false);
				return;
			}
				
			DebugLog.Add("GstPlayer: pipeline iterate leave");
		}
		
		private bool OnTimeout()
		{
			if(atEos && Eos != null) {
				atEos = false;
				Eos(this, new EventArgs());
			}
			
			if(pipeline == null || pipeline.State != ElementState.Playing)
				return true;
				
			if(Tick != null) {
				TickEventArgs args = new TickEventArgs();
				args.Position = time;
				Tick(this, args);
			}
			
			return true;
		}
		
		public bool Open(string uri)
		{
			if(finalizing) 
				return false;
			
			requestedState = ElementState.Ready;
			finalizing = false;
			atEos = false;

			if(uri == null)
				return false;
				
			if(pipeline != null)
				pipeline.SetState(ElementState.Null);
				
			if(!Construct())
				return false;
				
			this.uri = uri;
			src.SetProperty("location", uri);
			
			DebugLog.Add("GstPlayer: Opening '{0}'",
				StringUtil.UriEscape(uri));
			
			return true;
		}
		
		public void Close()
		{
			finalizing = true;
			Finalize(true);
		}
		
		private void Finalize(bool join)
		{
			DebugLog.Add("GstPlayer: finalizing");

			uri = null;
		
			if(pipeline == null) {
				finalizing = false;
				return;
			}

			requestedState = ElementState.Ready;
			
			if(join && thread != null && pipeline != null)
				thread.Join();
		
			finalizing = false;
			pipeline = null;
		
			// Thread sync interface problem
			//if(!join && Eos != null) {
			//	DebugLog.Add("GstPlayer: emitting Eos()");
			//	Eos(this, new EventArgs());	
			//}
			
			if(!join)
				atEos = true;
		}
		
		public void Play()
		{
			if(pipeline == null)
				return;
			
			if(requestedState == ElementState.Playing)
				return;

			requestedState = ElementState.Playing;
			thread = new System.Threading.Thread(new ThreadStart(Iterate));
			thread.Start();
		}
        
        public void Pause()
        {
        	if(pipeline == null)
        		return;
        
       		if(requestedState == ElementState.Playing)
       			requestedState = ElementState.Paused;	
       	}
       	
       	public bool Playing 
       	{ 
       		get { 
       			return requestedState == ElementState.Playing; 
       		} 
       	}
       	
       	public bool HasFile 
       	{ 
       		get { 
       			return uri != null; 
       		} 
       	}
       	
       	public bool Loaded
       	{
       		get {
       			return uri != null && pipeline != null;
       		}
       	}
       	
       	public long Position 
       	{ 
       		get { 
       			return time; 
       		} 
       	}
       	
	    public double Volume 
	    {
	        get { 
	        	return volumeValue; 
	        }
	        
	        set { 
				double newVolume = value / 100.0;
				if(newVolume > 1.0)
					volumeValue = 1.0;
				else if(newVolume < 0.0)
					volumeValue = 0.0;
				else
					volumeValue = newVolume;

				if(VolumeChanged != null) {
					VolumeChangedEventArgs args = new VolumeChangedEventArgs();
					args.Volume = volumeValue * 100.0;
					VolumeChanged(this, args);
				}
	        }
	    }
	    
	    public bool AtEos
	    {
	    	get {
	    		return atEos;
	    	}
	    }
	}
}
