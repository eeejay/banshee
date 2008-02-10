//
// Device.cs
//
// Author:
//   Ruben Vermeersch <ruben@savanne.be>
//
// Copyright (C) 2007-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Hal;
using Banshee.Collection;

namespace Banshee.Dap
{
	public abstract class AbstractDevice : IDevice
	{
		public event EventHandler Ejected;
		public event EventHandler Initialized;
		public event EventHandler MetadataUpdated;
		public event EventHandler TrackDownloaded;
		public event EventHandler TracksLoaded;
		public event EventHandler TrackRemoved;
		public event EventHandler TrackUploaded;
		
		
		private string name = "DAP";
		private string owner = "Owner";
		
		
		public virtual bool CanSetName {
			get { return false; }
		}
		
		public virtual bool CanSetOwner {
			get { return false; }
		}
		
		public virtual string Name {
			get { return name; }
			set {
				if (!CanSetName)
					throw new InvalidOperationException ();
				name = value;
			}
		}
		
		public virtual string Owner {
			get { return owner; }
			set {
				if (!CanSetOwner)
					throw new InvalidOperationException ();
			}
		}
		
		public virtual ulong Capacity {
			get { return 0; }
		}
		
		public virtual ulong FreeSpace {
			get { return 0; }
		}
		
		public virtual bool IsReadOnly {
			get { return false; }
		}
		
		public virtual bool IsPlaybackSupported {
			get { return false; }
		}
		
		public virtual void Dispose ()
		{
		}
		public abstract void DownloadTrack (TrackInfo track, string destination);
		public virtual void Eject ()
		{
			RaiseEjected();
		}
		public abstract bool Initialize (Device device);
		public abstract void LoadTracks ();
		public abstract void RemoveTrack (TrackInfo track);
		public abstract void UpdateMetadata (TrackInfo track);
		public abstract void UploadTrack (TrackInfo track);
		
		protected virtual void RaiseEjected()
		{
			RaiseEvent(Ejected, EventArgs.Empty);
		}
		protected virtual void RaiseInitialized()
		{
			RaiseEvent(Initialized, EventArgs.Empty);
		}
		protected virtual void RaiseMetadataUpdated()
		{
			RaiseEvent(MetadataUpdated, EventArgs.Empty);
		}
		protected virtual void RaiseTrackDownloaded()
		{
			RaiseEvent(TrackDownloaded, EventArgs.Empty);
		}
		protected virtual void RaiseTracksLoaded()
		{
			RaiseEvent(TracksLoaded, EventArgs.Empty);
		}
		protected virtual void RaiseTrackUploaded()
		{
			RaiseEvent(TrackUploaded, EventArgs.Empty);
		}
		protected virtual void RaiseTrackRemoved()
		{
			RaiseEvent(TrackRemoved, EventArgs.Empty);
		}
		private void RaiseEvent(EventHandler h, EventArgs e)
		{
			if (h != null)
				h (this, e);
		}
	}
}
