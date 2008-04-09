//
// MtpSource.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.Collections.Generic;
using System.Threading;
using Mono.Unix;

using Hyena;
using Hyena.Collections;
using Mtp;

using Banshee.Base;
using Banshee.ServiceStack;
using Banshee.Library;
using Banshee.Sources;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Hardware;

namespace Banshee.Dap.Mtp
{
    public class MtpSource : DapSource
    {
        // libmtp only lets us have one device connected at a time
        private static MtpSource mtp_source;

		private MtpDevice mtp_device;
        //private bool supports_jpegs = false;

        public MtpSource () : base ()
        {
        }

        protected override bool Initialize (IDevice device)
        {
            Log.DebugFormat ("MTP testing device {0}", device.Uuid);
            if (MediaCapabilities == null || !MediaCapabilities.IsType ("mtp")) {
                Log.DebugFormat ("FAILED");
                return false;
            }

            // libmtp only allows us to have one MTP device active
            if (mtp_source != null) {
                Log.Information (
                    Catalog.GetString ("MTP Support Ignoring Device"),
                    Catalog.GetString ("Banshee's MTP audio player support can only handle one device at a time."),
                    true
                );
				return false;
            }

            string serial = "";//hal_device ["usb.serial"];

			List<MtpDevice> devices = null;
			try {
				devices = MtpDevice.Detect ();
			} catch (TypeInitializationException e) {
                Log.Exception (e);
				Log.Error (
                    Catalog.GetString ("Error Initializing MTP Device Support"),
                    Catalog.GetString ("There was an error intializing MTP device support.  See http://www.banshee-project.org/Guide/DAPs/MTP for more information.")
                );
				return false;
			} catch (Exception e) {
                Log.Exception (e);
				//ShowGeneralExceptionDialog (e);
				return false;
			}

            if (devices == null || devices.Count == 0) {
				Log.Error (
                    Catalog.GetString ("Error Finding MTP Device Support"),
                    Catalog.GetString ("An MTP device was detected, but Banshee was unable to load support for it.")
                );
            } else {
                string mtp_serial = devices[0].SerialNumber;
                if (!String.IsNullOrEmpty (mtp_serial) && !String.IsNullOrEmpty (serial)) {
                    if (mtp_serial.Contains (serial)) {
                        mtp_device = devices[0];
                        mtp_source = this;
                    }
                }

                if (mtp_device == null) {
                    Log.Information(
                        Catalog.GetString ("MTP Support Ignoring Device"),
                        Catalog.GetString ("Banshee's MTP audio player support can only handle one device at a time."),
                        true
                    );
                }
            }

            if (mtp_device == null) {
                return false;
            }

			/*Log.Debug ("Loading MTP Device",
                String.Format ("Name: {0}, ProductID: {1}, VendorID: {2}, Serial: {3}",
                    hal_name, product_id, vendor_id, serial
                )
            );*/

            Initialize ();

            // TODO differentiate between Audio Players and normal Disks, and include the size, eg "2GB Audio Player"?
            //GenericName = Catalog.GetString ("Audio Player");

            // TODO construct device-specific icon name as preferred icon
            //Properties.SetStringList ("Icon.Name", "media-player");

            //SetStatus (String.Format (Catalog.GetString ("Loading {0}"), Name), false);
            /*DatabaseImportManager importer = new DatabaseImportManager (this);
            importer.KeepUserJobHidden = true;
            importer.ImportFinished += delegate  { HideStatus (); };
            importer.QueueSource (BaseDirectory);*/

            return true;
        }

        public override void Import ()
        {
            //new LibraryImportManager (true).QueueSource (BaseDirectory);
        }

        public override long BytesUsed {
            //get { return BytesCapacity - volume.Available; }
            get { return 0; }
        }
        
        public override long BytesCapacity {
            //get { return (long) volume.Capacity; }
            get { return 0; }
        }

        protected override bool IsReadOnly {
            //get { return volume.IsReadOnly; }
            get { return false; }
        }

        protected override void DeleteTrack (DatabaseTrackInfo track)
        {
            /*try {
                Banshee.IO.Utilities.DeleteFileTrimmingParentDirectories (track.Uri);
            } catch (System.IO.FileNotFoundException) {
            } catch (System.IO.DirectoryNotFoundException) {
            }*/
        }

        protected override void Eject ()
        {
            /*if (volume.CanUnmount)
                volume.Unmount ();

            if (volume.CanEject)
                volume.Eject ();
                */
        }
    }
}
