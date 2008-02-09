//
// IDevice.cs
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
using Banshee.Collection;
using Hal;

namespace Banshee.Dap
{
    public interface IDevice: IDisposable {
		
		event EventHandler Ejected;
		event EventHandler Initialized;

		event EventHandler MetadataUpdated;
		event EventHandler TrackDownloaded;
		event EventHandler TracksLoaded;
		event EventHandler TrackRemoved;
		event EventHandler TrackUploaded;

		
		void DownloadTrack (TrackInfo track);       // Should be TrackInfo, not 'object'
		void LoadTracks ();                      // Should be TrackInfo, not 'object'
		void RemoveTrack (TrackInfo track);         // Should be TrackInfo, not 'object'
		void UpdateMetadata (TrackInfo track);      // Should be TrackInfo, not 'object'
		void UploadTrack (TrackInfo track);         // Should be TrackInfo, not 'object'

		void Eject ();
		bool Initialize (Device device);
		
		//bool CanSetName { get; }
		//bool CanSetOwner { get; }
		string Name { get; set; }
		string Owner { get; set; }
		ulong Capacity { get; }
		ulong FreeSpace { get; }
		bool IsReadOnly { get; }
		bool IsPlaybackSupported { get; }
	}
}
