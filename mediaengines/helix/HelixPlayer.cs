// created on 7/31/2005 at 3:35 PM

using System;
using System.Threading;
using Helix;

namespace Sonance
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
		
		public string EngineName
		{
			get {
				return "Helix Engine (hxclientkit)";
			}
		}
		
		public HelixPlayer()
		{
			player = new HxPlayer();
			player.ContentConcluded += OnContentConcluded;
			player.ErrorOccurred += OnErrorOccurred;
			
			playbackThread = new Thread(new ThreadStart(ThreadedIterate));
			playbackThread.Start();
		}
		
		public bool Open(ITrackInfo ti)
		{
			loaded = player.OpenUri("file://" + ti.Uri);
			return loaded;
		}
		
		public void Play()
		{
			if(player.State == HxContentState.Stopped 
				|| player.State == HxContentState.Paused)
				player.Play();
		}
		
		public void Pause()
		{
			if(player.State == HxContentState.Playing)
				player.Pause();
		}
		
		public void Shutdown()
		{
			
		}
		
		public bool Playing 
		{
			get {
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
		
		private void ThreadedIterate()
		{
			while(player.Iterate());
		}
		
		private void OnContentConcluded(object o, HxPlayerArgs args)
		{
			EmitEndOfStream();
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
