// created on 7/31/2005 at 3:35 PM

using System;
using System.Threading;
using Helix;

namespace Banshee
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
				return "Helix Framework Engine (hxclientkit)";
			}
		}
		
		public string EngineDetails
		{
			get {
				return "The Helix Engine provides multimedia control through " +
				"the Helix Multimedia Framework, sponsored by RealNetworks. " +
				"The engine can play any file that RealPlayer can. Install " + 
				"RealPlayer for best results.";
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
		
		public HelixPlayer()
		{
		
		}
		
		public void Initialize()
		{
			player = new HxPlayer();
			player.ContentConcluded += OnContentConcluded;
			player.ErrorOccurred += OnErrorOccurred;
			player.ContentStateChanged += OnContentStateChanged;
			
			//playbackThread = new Thread(new ThreadStart(ThreadedIterate));
			//playbackThread.Start();
			
			GLib.Timeout.Add(100, new GLib.TimeoutHandler(ThreadedIterate));
		}
		
		public void TestInitialize()
		{
			HxPlayer testplayer = new HxPlayer();
			uint testvol = testplayer.Volume;
			testplayer.Volume = testvol;
		//	testplayer.OpenUri("file:///dev/null");
		//	testplayer.Play();
		///	testplayer.Stop();
		}
		
		public bool Open(ITrackInfo ti)
		{
			loaded = player.OpenUri("file://" + ti.Uri);
			Console.WriteLine("Loaded URI: " + loaded);
			return loaded;
		}
		
		public void Play()
		{
			if(player.State == HxContentState.Stopped 
				|| player.State == HxContentState.Paused) {
				Console.WriteLine("Playing");
				player.Play();}
		}
		
		public void Pause()
		{
			if(player.State == HxContentState.Playing)
				player.Pause();
		}
		
		public void Shutdown()
		{
			//shutdown = true;
		}
		
		public bool Playing 
		{
			get {
				Console.WriteLine("State: " + player.State);
				return player.State == HxContentState.Playing;
			}
		}
		
		public long Position
		{
			get {
				return (long)player.Position;
			}
			
			set {
				player.Position = (int)value;
			}
		}
		
		public bool Loaded
		{
			get {
				return loaded;
			}
		}
		
		public double Volume
		{
			get {
				return (double)player.Volume / 100.0;
			}
			
			set {
				uint newVolume = (uint)value * 100;
				
				if(newVolume > 100)
					player.Volume = 100;
				else if(newVolume < 0)
					player.Volume = 0;
				else
					player.Volume = newVolume;
				
				PlayerEngineVolumeChangedArgs args = 
						new PlayerEngineVolumeChangedArgs();
				args.Volume = Volume;
				EmitVolumeChanged(args);
			}
		}
		
		// --- //
		
		private bool ThreadedIterate()
		{
			if(Playing) {
			Console.WriteLine("Pumping HxClientHengine");
				player.Iterate();
				Console.WriteLine("Pumped HxClientHengine");
				PlayerEngineIterateArgs args = new PlayerEngineIterateArgs();
				args.Position = Position;
				EmitIterate(args);
			} else {
				Console.WriteLine("Idling");
			}
			//return true;
			return !shutdown;
		}
		
		private void OnContentConcluded(object o, HxPlayerArgs args)
		{
			EmitEndOfStream();
		}
		
		private void OnContentStateChanged(object o, ContentStateChangedArgs args)
		{
			Console.WriteLine("Setting New State: " + args.NewState);
		}
		
		private void OnErrorOccurred(object o, ErrorOccurredArgs args)
		{
			PlayerEngineErrorArgs eargs = new PlayerEngineErrorArgs();
			eargs.Error = args.Error;
			EmitError(eargs);
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
