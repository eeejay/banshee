using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Banshee.Cdrom.Nautilus.Interop
{
    internal class BurnRecorderTrack : GLib.Opaque, IDisposable
    {
        public BurnRecorderTrack(IntPtr raw) : base(raw)
        {
        }
        
        public BurnRecorderTrack(string filename, BurnRecorderTrackType type) 
            : base(nautilus_burn_glue_create_track(filename, type))
        {
        }
        
        ~BurnRecorderTrack()
        {
            Dispose(false);
        }
        
        public new void Dispose() 
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        private void Dispose(bool disposing)
        {
            if(Raw != IntPtr.Zero) {
                nautilus_burn_recorder_track_free(Handle);
                Raw = IntPtr.Zero;
               }
        }
        
        [DllImport("libbanshee")]
        private static extern IntPtr nautilus_burn_glue_create_track(string filename,
            BurnRecorderTrackType type);
        
           [DllImport("libnautilus-burn")]
        private static extern void nautilus_burn_recorder_track_free(IntPtr raw);
    }
}
