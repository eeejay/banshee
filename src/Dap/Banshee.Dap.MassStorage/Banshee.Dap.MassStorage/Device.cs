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

using Banshee.Dap;

namespace Banshee.Dap.MassStorage {
    public class Device : AbstractDevice {
		
		public override void DownloadTrack (TrackInfo track, string destination)
		{
			// Transfer the specified track from the device to the specified file path
			throw new System.NotImplementedException ();
		}

		public override bool Initialize (Device device)
		{
			// Initialize the MassStorage from the hal device
			throw new System.NotImplementedException ();
		}

		public override void LoadTracks ()
		{
			// Load all tracks from the device library 
			throw new System.NotImplementedException ();
		}

		public override void RemoveTrack (TrackInfo track)
		{
			// Remove the specified track from the device
			throw new System.NotImplementedException ();
		}

		public override void UpdateMetadata (TrackInfo track)
		{
			// Update the metadata for the specified track
			throw new System.NotImplementedException ();
		}

		public override void UploadTrack (TrackInfo track)
		{
			// Transfer the specified track onto the device
			throw new System.NotImplementedException ();
		}
    }
}
