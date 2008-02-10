// AsyncDevice.cs created with MonoDevelop
// User: alan at 18:20 10/02/2008
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Threading;

namespace Banshee.Dap
{
	public class AsyncDevice : AbstractDevice
	{
		private IDevice device;
		
		public override bool CanSetName {
			get { return device.CanSetName; }
		}

		public override bool CanSetOwner {
			get { return device.CanSetOwner; }
		}

		public override string Name {
			get { return device.Name; }
			set { device.Name = value; }
		}

		public override string Owner {
			get { return device.Owner; }
			set { device.Owner = value; }
		}

		public override ulong Capacity {
			get { return device.Capacity; }
		}

		public override ulong FreeSpace {
			get { return device.FreeSpace; }
		}

		public override bool IsReadOnly {
			get { return device.IsReadOnly; }
		}

		public override bool IsPlaybackSupported {
			get { return device.IsPlaybackSupported; }
		}

		
		public AsyncDevice (IDevice device)
		{
			if (device == null)
				throw new ArgumentNullException ("device");
			
			this.device = device;
			device.Ejected += delegate { RaiseEjected (); };
			device.Initialized += delegate { RaiseInitialized (); };
			device.MetadataUpdated += delegate { RaiseMetadataUpdated (); };
			device.TrackDownloaded += delegate { RaiseTrackDownloaded (); };
			device.TrackRemoved += delegate { RaiseTrackRemoved (); };
			device.TracksLoaded += delegate { RaiseTracksLoaded (); };
			device.TrackUploaded += delegate { RaiseTrackUploaded (); };
		}

		public override void Dispose ()
		{
			device.Dispose ();
		}

		public override void DownloadTrack (Banshee.Collection.TrackInfo track, string destination)
		{
			ThreadPool.QueueUserWorkItem (delegate {
				device.DownloadTrack (track, destination);
			});
		}

		public override void LoadTracks ()
		{
			ThreadPool.QueueUserWorkItem (delegate {
				device.LoadTracks ();
			});
		}

		public override void RemoveTrack (Banshee.Collection.TrackInfo track)
		{
			ThreadPool.QueueUserWorkItem (delegate {
				device.RemoveTrack (track);
			});
		}

		public override void UpdateMetadata (Banshee.Collection.TrackInfo track)
		{
			ThreadPool.QueueUserWorkItem (delegate {
				device.UpdateMetadata (track);
			});
		}

		public override void UploadTrack (Banshee.Collection.TrackInfo track)
		{
			ThreadPool.QueueUserWorkItem (delegate {
				device.UploadTrack (track);
			});
		}

		public override void Eject ()
		{
			ThreadPool.QueueUserWorkItem (delegate {
				device.Eject ();
			});
		}

		public override bool Initialize (Hal.Device halDevice)
		{
			return device.Initialize (halDevice);
		}
	}
}
