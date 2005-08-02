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
using System.Text.RegularExpressions; 
using Gst;
using GLib;
	
namespace Sonance
{	
	public class GstPlayer : IPlayerEngine
	{
		private Bin pipeline = null;
		
		private Element sink;
		private Element src;
		private Element decoder;
		private Element volume;
	
		private System.Threading.Thread thread;
		
		private ElementState requestedState = ElementState.Ready;
		
		private string uri = null;
		//private bool playing = false;
		private long time;
		private double volumeValue = 0.8;
		
		//private uint timeoutHandle = 0;
		private bool finalizing = false;
		private bool atEos = false;
		
		public event PlayerEngineErrorHandler Error;
		public event PlayerEngineVolumeChangedHandler VolumeChanged;
		public event PlayerEngineIterateHandler Iterate;
		public event EventHandler EndOfStream;

		public string EngineName
		{
			get {
				return "GStreamer CLI Engine (gst-sharp)";
			}
		}

		public GstPlayer()
		{
			Element dummy;
			string [] args = {""};
			Gst.Application.Init("Sonance", ref args);
			dummy = Gst.ElementFactory.Make("fakesink", "fakesink");
			Gst.SchedulerFactory.Make(null, dummy);
		}
		
		private bool Construct()
		{
			pipeline = new Pipeline("pipeline");
		
			src = ElementFactory.Make("gnomevfssrc", "gnomevfssrc");
			if(src == null) {
				throw new ApplicationException("Could not construct gnomevfssrc element");
			}
			
			decoder = ElementFactory.Make("spider", "spider");
			if(decoder == null) {
				throw new ApplicationException("Could not construct a decoder element");
			}
			
			volume = ElementFactory.Make("volume", "volume");
			if(volume == null) {
				throw new ApplicationException("Could not construct a volume element");
			}
			
			volume.SetProperty("volume", volumeValue);
		
			sink = Gst.Gconf.DefaultAudioSink;
			if(sink == null) {
				throw new ApplicationException("Could not construct an output sink element");
			}

			pipeline.Add(src);
			pipeline.Add(decoder);
			pipeline.Add(volume);
			pipeline.Add(sink);

			src.Link(decoder);
			decoder.Link(volume);
			volume.Link(sink);
			
			GLib.Timeout.Add(250, 
				new TimeoutHandler(OnTimeout));
			
			return true;
		}
		
		public void IteratePipeline() 
		{
			int format;
			pipeline.SetState(requestedState);
			ElementState currentPlayingState = requestedState;
			double lastVolumeValue = 0;
			
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
				
		}
		
		private bool OnTimeout()
		{
			if(atEos) {
				atEos = false;
				EmitEndOfStream();
			}
			
			if(pipeline == null || pipeline.State != ElementState.Playing)
				return true;
				
			PlayerEngineIterateArgs args = new PlayerEngineIterateArgs();
			args.Position = time;
			EmitIterate(args);
			
			return true;
		}
		
		public bool Open(ITrackInfo ti)
		{
			Shutdown();
			
			if(finalizing) 
				return false;
			
			requestedState = ElementState.Ready;
			finalizing = false;
			atEos = false;

			if(ti == null)
				return false;
				
			if(pipeline != null)
				pipeline.SetState(ElementState.Null);
				
			if(!Construct())
				return false;
				
			uri = ti.Uri;
			src.SetProperty("location", uri);
			
			return true;
		}
		
		public void Shutdown()
		{
			finalizing = true;
			Finalize(true);
		}
		
		private void Finalize(bool join)
		{

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
			
			if(!join) {
				atEos = true;
			}
		}
		
		public void Play()
		{
			if(pipeline == null)
				return;
			
			if(requestedState == ElementState.Playing)
				return;

			requestedState = ElementState.Playing;
			thread = new System.Threading.Thread(new ThreadStart(IteratePipeline));
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
       		
       		set {
       			
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
					PlayerEngineVolumeChangedArgs args = new PlayerEngineVolumeChangedArgs();
					args.Volume = volumeValue * 100.0;
					EmitVolumeChanged(args);
				}
	        }
	    }
	    
	    public bool AtEos
	    {
	    	get {
	    		return atEos;
	    	}
	    }
	  
		protected void EmitError(PlayerEngineErrorArgs args)
		{
			PlayerEngineErrorHandler handler = Error;
			if(handler != null)
				handler(this, args);
		}
			
		protected void EmitVolumeChanged(PlayerEngineVolumeChangedArgs args)
		{
			PlayerEngineVolumeChangedHandler handler = VolumeChanged;
			if(handler != null)
				handler(this, args);
		} 
		
		protected void EmitIterate(PlayerEngineIterateArgs args)
		{
			PlayerEngineIterateHandler handler = Iterate;
			if(handler != null)
				handler(this, args);
		}
		
		protected void EmitEndOfStream()
		{
			EventHandler handler = EndOfStream;
			if(handler != null)
				handler(this, new EventArgs());
		}
	}
	
	public class StringUtil
	{
		public static string EntityEscape(string str)
		{
			if(str == null)
				return null;
				
			return str.Replace("&", "&amp;");
		}
	
		private static string RegexHexConvert(Match match)
		{
			int digit = Convert.ToInt32(match.Groups[1].ToString(), 16);
			return Convert.ToChar(digit).ToString();
		}	
				
		public static string UriEscape(string uri)
		{
			return Regex.Replace(uri, "%([0-9A-Fa-f][0-9A-Fa-f])", 
				new MatchEvaluator(RegexHexConvert));
		}
		
		public static string UriToFileName(string uri)
		{
			uri = UriEscape(uri).Trim();
			if(!uri.StartsWith("file://"))
				return uri;
				
			return uri.Substring(7);
		}
		
		public static string UcFirst(string str)
		{
			return Convert.ToString(str[0]).ToUpper() + str.Substring(1);
		}
	}
}
