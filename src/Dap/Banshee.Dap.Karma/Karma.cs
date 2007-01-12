/***************************************************************************
 *  Karma.cs
 *
 *  Copyright (C) 2006 Bob Copeland <me@bobcopeland.com>
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
using System.IO;
using System.Collections;
using Mono.Unix;
using Hal;
using KarmaLib=Karma;
using Banshee.Dap;
using Banshee.Base;

public static class PluginModuleEntry
{
    public static Type [] GetTypes()
    {
        return new Type [] {
            typeof(Banshee.Dap.Karma.KarmaDap)
        };
    }
}

namespace Banshee.Dap.Karma
{
    [DapProperties(DapType = DapType.NonGeneric)]
    [SupportedCodec(CodecType.Mp3)]
    [SupportedCodec(CodecType.Wma)]
    [SupportedCodec(CodecType.Ogg)]

    public sealed class KarmaDap : DapDevice
    {
        private KarmaLib.Device device;
        private Hal.Device hal_device;
        private int sync_count;
        private int sync_completed;
        private Queue remove_queue = new Queue();

        public override InitializeResult Initialize(Hal.Device halDevice)
        {
            if(!halDevice.PropertyExists("block.device") ||
               !halDevice.GetPropertyBoolean("block.is_volume") ||
               !IsKarma(halDevice)) {
                return InitializeResult.Invalid;
            } else if(!halDevice.GetPropertyBoolean("volume.is_mounted")) {
                return InitializeResult.WaitForVolumeMount;
            }

            hal_device = halDevice;

            if (LoadKarma() == InitializeResult.Invalid) {
                return InitializeResult.Invalid;
            }

            ReloadDatabase();

            base.Initialize(halDevice);
            return InitializeResult.Valid;
        }

        private bool IsKarma(Hal.Device halDevice)
        {
            return halDevice["block.storage_device"].IndexOf("Rio_Karma") >= 0;
        }

        private InitializeResult LoadKarma() {
            try {
                device = new KarmaLib.Device(hal_device["volume.mount_point"]);
            } catch(Exception e) {
                Console.WriteLine(e);
                return InitializeResult.Invalid;
            }
            return InitializeResult.Valid;
        }

        public override Gdk.Pixbuf GetIcon(int size)
        {
            Gdk.Pixbuf icon = IconThemeUtils.LoadIcon(
                    "multimedia-player-rio-karma", size);
            if (icon != null) {
                return icon;
            }

            return base.GetIcon(size);
        }

        private void ReloadDatabase()
        {
            ClearTracks(false);
            foreach (KarmaLib.Song song in device.GetSongs()) {
                KarmaDapTrackInfo track = new KarmaDapTrackInfo(song,
                    hal_device["volume.mount_point"]);
                AddTrack(track);
            }
        }

        public override void AddTrack(TrackInfo track) {
            if (track is KarmaDapTrackInfo ||
                !TrackExistsInList(track, Tracks)) {
                tracks.Add(track);
                OnTrackAdded(track);
            }
        }

        protected override void OnTrackRemoved(TrackInfo track) {
            if (!(track is KarmaDapTrackInfo))
                return;
            remove_queue.Enqueue(track);
        }

        public override void Synchronize()
        {
            sync_count = 0;
            sync_completed = 0;

            int remove_total = remove_queue.Count;

            while (remove_queue.Count > 0) {
                KarmaDapTrackInfo track = remove_queue.Dequeue() as
                    KarmaDapTrackInfo;
                UpdateSaveProgress(Catalog.GetString("Synchronizing Device"),
                    Catalog.GetString("Removing Songs"),
                    (double) (remove_total - remove_queue.Count) /
                    (double) remove_total);
                device.DeleteSong(track.TrackId);
            }

            foreach (TrackInfo track in Tracks) {
                if (track is KarmaDapTrackInfo)
                    continue;

                if (track.Uri.IsFile) {
                    device.QueueAddSong(track.Uri.LocalPath);
                    sync_count++;
                }
            }
            device.ProgressChanged += OnFileUploaded;
            device.Save();
            ReloadDatabase();
            FinishSave();
        }

        private void OnFileUploaded(KarmaLib.Song song)
        {
            sync_completed++;
            string message = String.Format("{0} - {1}", song.Artist,
                song.Title);

            UpdateSaveProgress(Catalog.GetString("Synchronizing Device"),
                message, (double)sync_completed/(double)sync_count);
        }

        public override string Name {
            get {
                return "Karma";
            }
        }

        public override ulong StorageCapacity {
            get {
                return device.GetStorageDetails().StorageSize;
            }
        }

        public override ulong StorageUsed {
            get {
                KarmaLib.StorageDetails details = device.GetStorageDetails();
                return details.StorageSize - details.FreeSpace;
            }
        }

        public override bool IsReadOnly {
            get {
                return false;
            }
        }

        public override bool IsPlaybackSupported {
            get {
                return false;
            }
        }
    }
}
