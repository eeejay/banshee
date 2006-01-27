/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  HelixPlayer.cs
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
using Mono.Unix;

using Helix;
using Banshee.Base;
using Banshee.MediaEngine;

namespace Banshee.MediaEngine.Helix
{
	public class HelixPlayer : IPlayerEngine
	{
		private HxPlayer player;
		private bool loaded;
		private Thread playbackThread;
		
		public event PlayerEngineErrorHandler Error;
		public event PlayerEngineVolumeChangedHandler VolumeChanged;
		public event PlayerEngineIterateHandler Iterate;
		public event EventHandler EndOfStream;
		
		private DateTime lastIterateEmit;
		private Thread iterateThread;
		
		private TrackInfo track;
		
		private bool disabled;
		private bool shutdown;
		
		public bool Disabled {
			get { return disabled;  }
			set { disabled = value; }
		}
		
		public string ConfigName
		{
			get {
				return "helix";
			}
		}
		
		public string EngineName
		{
			get {
				return "Helix";
			}
		}
		
		public string EngineLongName
		{
			get {
				return Catalog.GetString("Helix Framework Engine (hxclientkit)");
			}
		}
		
		public string EngineDetails
		{
			get {
				return Catalog.GetString(
					"The Helix Engine provides multimedia control through " +
					"the Helix Multimedia Framework, sponsored by RealNetworks. " +
					"The engine can play any file that RealPlayer can. Install " +
					"RealPlayer for best results.");
			}
		}
		
		public int MajorVersion
		{
			get {
				return 0;
			}
		}
		
		public int MinorVersion
		{
			get {
				return 1;
			}
		}
		
		public string AuthorName
		{
			get {
				return "Aaron Bockover";
			}
		}
		
		public string AuthorEmail
		{
			get {
				return "aaron@aaronbock.net";
			}
		}
		
		public string [] SupportedExtensions {
		  get {
		      return null;
		  }
	   }
		
		public void Initialize()
		{
			player = new HxPlayer();
			player.Muted = false;
			player.ContentConcluded += OnContentConcluded;
			player.ErrorOccurred += OnErrorOccurred;
			player.ContentStateChanged += OnContentStateChanged;
			
			iterateThread = new Thread(new ThreadStart(DoIterate));
			iterateThread.IsBackground = true;
			iterateThread.Start();
		}
		
		public void TestInitialize()
		{
			new HxPlayer();
		}
		
		public void Dispose()
		{
			shutdown = true;
		}
		
		public bool Open(TrackInfo ti, Uri uri)
		{
			if(!ti.CanPlay) {
				EmitEndOfStream();
				return false;
			}
			
			loaded = player.OpenUri(uri.AbsoluteUri);
			
			if(loaded)
				track = ti;
			else
				track = null;

			return loaded;
		}
		
		public void Close()
		{
			player.Stop();
		}
		
		public void Play()
		{
			if(player.State == HxContentState.Stopped 
				|| player.State == HxContentState.Paused) {
				player.Play();
			}
		}
		
		public void Pause()
		{
			if(player.State == HxContentState.Playing) {
				player.Pause();
			}
		}
		
		public bool Playing 
		{
			get {
				return player.State == HxContentState.Playing;
			}
		}
		
		public uint Position
		{
			get {
				return player.Position / 1000;
			}
			
			set {
				player.Position = value * 1000;
			}
		}
		
		public uint Length
		{
			get {
				return player.Length / 1000;
			}
		}
		
		public bool Loaded
		{
			get {
				return loaded;
			}
		}
		
		public ushort Volume
		{
			get {
				return player.Volume;
			}
			
			set {
				player.Volume = value;
				
				PlayerEngineVolumeChangedArgs args = 
					new PlayerEngineVolumeChangedArgs();
				args.Volume = Volume;
				EmitVolumeChanged(args);
			}
		}
		
		public TrackInfo Track
		{
			get {
				return track;
			}
		}
		
		// --- //
		
		private void DoIterate()
		{
			while(!shutdown) {
				player.Iterate();
			
				// emit iterate signal only once every half second
				if(DateTime.Now - lastIterateEmit >= 
					new TimeSpan(0, 0, 0, 0, 500) && Playing) {
					PlayerEngineIterateArgs args = 
						new PlayerEngineIterateArgs();
					args.Position = Position;
					EmitIterate(args);
					lastIterateEmit = DateTime.Now;
				}
	
				Thread.Sleep(HxPlayer.PumpEventDelay);
			}
		}
		
		private void OnContentConcluded(object o, HxPlayerArgs args)
		{
			EmitEndOfStream();
		}
		
		private void OnContentStateChanged(object o, 
			ContentStateChangedArgs args)
		{
			Console.WriteLine("Helix Content State Changed: {0} -> {1}",
				args.NewState, args.OldState);
		}
		
		private void OnErrorOccurred(object o, ErrorOccurredArgs args)
		{
			PlayerEngineErrorArgs eargs = new PlayerEngineErrorArgs();
			eargs.Error = args.Error;
			EmitError(eargs);
			
			Console.WriteLine("HxError: " + args.Error + ": " + args.UserError);
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
}
