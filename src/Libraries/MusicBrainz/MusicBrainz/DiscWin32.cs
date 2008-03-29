/***************************************************************************
 *  DiscWin32.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

// This is based on $Id: disc_win32.c 8506 2006-09-30 19:02:57Z luks $

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace MusicBrainz
{
    internal sealed class DiscWin32 : LocalDisc
    {
        [DllImport ("winmm")]
        static extern Int32 mciSendString (String command,
                                           StringBuilder buffer,
                                           Int32 bufferSize,
                                           IntPtr hwndCallback);

        [DllImport ("winmm")]
        static extern Int32 mciGetErrorString (Int32 errorCode,
                                               StringBuilder errorText,
                                               Int32 errorTextSize);
        
        delegate void MciCall (string result);

        internal DiscWin32 (string device)
        {
            string device_string = device.Length == 0
                ? "cdaudio" : string.Format ("{0} type cdaudio", device);

            string alias = string.Format ("musicbrainz_cdio_{0}_{1}",
                Environment.TickCount, Thread.CurrentThread.ManagedThreadId);

            MciClosure (
                "sysinfo cdaudio quantity wait",
                "Could not get the list of CD audio devices",
                delegate (string result) {
                    if (int.Parse (result.ToString ()) <= 0)
                        throw new Exception ("No CD audio devices present.");
                });

            MciClosure (
                string.Format ("open {0} shareable alias {1} wait", device_string, alias),
                string.Format ("Could not open device {0}", device),
                null);

            MciClosure (
                string.Format ("status {0} number of tracks wait", alias),
                "Could not read number of tracks",
                delegate (string result) {
                    FirstTrack = 1;
                    LastTrack = byte.Parse (result);
                });

            MciClosure (
                string.Format ("set {0} time format msf wait", alias),
                "Could not set time format",
                null);

            for (int i = 1; i <= LastTrack; i++)
                MciClosure (
                    string.Format ("status {0} position track {1} wait", alias, i),
                    string.Format ("Could not get position for track {0}", i),
                    delegate (string result) {
                        TrackOffsets [i] =
                            int.Parse (result.Substring (0,2)) * 4500 +
                            int.Parse (result.Substring (3,2)) * 75 +
                            int.Parse (result.Substring (6,2));
                    });

            MciClosure (
                string.Format ("status {0} length track {1} wait", alias, LastTrack),
                "Could not read the length of the last track",
                delegate (string result) {
                    TrackOffsets [0] =
                        int.Parse (result.Substring (0, 2)) * 4500 +
                        int.Parse (result.Substring (3, 2)) * 75 +
                        int.Parse (result.Substring (6, 2)) +
                        TrackOffsets [LastTrack] + 1;
                });

            MciClosure (
                string.Format ("close {0} wait", alias),
                string.Format ("Could not close device {0}", device),
                null);
            
            Init ();
        }

        static StringBuilder mci_result = new StringBuilder (128);
        static StringBuilder mci_error = new StringBuilder (256);
        static void MciClosure (string command, string failure_message, MciCall code)
        {
            int ret = mciSendString (command, mci_result, mci_result.Capacity, IntPtr.Zero);
            if (ret != 0) {
                mciGetErrorString (ret, mci_error, mci_error.Capacity);
                throw new Exception (string.Format ("{0} : {1}", failure_message, mci_error.ToString ()));
            } else if (code != null) code (mci_result.ToString ());
        }
    }
}
