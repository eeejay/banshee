using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Banshee.Cdrom.Nautilus.Interop 
{
    internal class BurnDrive : GLib.Opaque, IDisposable
    {
        public BurnDrive(IntPtr raw) : base(raw) 
        {
        }
        
        [DllImport("libbanshee")]
        static extern IntPtr nautilus_glue_burn_drive_get_for_device(string device_path);

        public BurnDrive (string device_path) : base(nautilus_glue_burn_drive_get_for_device(device_path))
        {
        }

        [DllImport("libbanshee")]
        private static extern void nautilus_burn_glue_drive_free(IntPtr raw);

        ~BurnDrive()
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
                nautilus_burn_glue_drive_free(Handle);
                Raw = IntPtr.Zero;
            }
        }
    
        [DllImport("libnautilus-burn")]
        private static extern bool nautilus_burn_drive_lock(IntPtr raw, string reason, 
            string reason_for_failure);

        public bool Lock(string reason, string reason_for_failure) 
        {
            return nautilus_burn_drive_lock(Handle, reason, reason_for_failure);
        }

        [DllImport("libnautilus-burn")]
        private static extern bool nautilus_burn_drive_unlock(IntPtr raw);

        public bool Unlock() 
        {
            return nautilus_burn_drive_unlock(Handle);
        }

        [DllImport("libnautilus-burn")]
        private static extern bool nautilus_burn_drive_eject(IntPtr raw);

        public bool Eject() 
        {
            return nautilus_burn_drive_eject(Handle);
        }

        [DllImport("libnautilus-burn")]
        private static extern bool nautilus_burn_drive_unmount(IntPtr raw);

        public bool Unmount() 
        {
            return nautilus_burn_drive_unmount(Handle);
        }

        [DllImport("libnautilus-burn")]
        private static extern bool nautilus_burn_drive_door_is_open(IntPtr raw);

        public bool DoorIsOpen() 
        {
            return nautilus_burn_drive_door_is_open(Handle);
        }

        [DllImport("libnautilus-burn")]
        private static extern bool nautilus_burn_drive_equal(IntPtr raw, ref BurnDrive drive);

        public override bool Equals(object o) 
        {
            if(o is BurnDrive) {
                BurnDrive drive = (BurnDrive)o;
                return nautilus_burn_drive_equal(Raw, ref drive);
            } else {
                return false;
            }
        }
        
        public override int GetHashCode()
        {
            return CdRecordId.GetHashCode();
        }
        
        [DllImport("libbanshee")]
        private static extern long nautilus_glue_burn_drive_get_media_capacity(IntPtr raw);

        public long MediaCapacity { 
            get { return nautilus_glue_burn_drive_get_media_capacity(Handle); }
        }

        [DllImport("libnautilus-burn")]
        private static extern int nautilus_burn_drive_get_media_type(IntPtr raw);

        public BurnMediaType MediaType { 
            get { return (BurnMediaType)nautilus_burn_drive_get_media_type(Handle); }
        }

        [DllImport ("libbanshee")]
        private static extern string nautilus_burn_glue_drive_get_id(IntPtr drive);

        public string CdRecordId {
            get { return nautilus_burn_glue_drive_get_id(Handle); }
        }

        [DllImport ("libbanshee")]
        private static extern string nautilus_burn_glue_drive_get_display_name(IntPtr drive);

        public string DisplayName {
            get { return nautilus_burn_glue_drive_get_display_name(Handle); }
        }

        [DllImport ("libbanshee")]
        private static extern int nautilus_burn_glue_drive_get_max_read_speed(IntPtr drive);

        public int MaxReadSpeed {
            get { return nautilus_burn_glue_drive_get_max_read_speed(Handle); }
        }

        [DllImport ("libbanshee")]
        private static extern int nautilus_burn_glue_drive_get_max_write_speed(IntPtr drive);

        public int MaxWriteSpeed {
            get { return nautilus_burn_glue_drive_get_max_write_speed(Handle); }
        }

        [DllImport ("libbanshee")]
        private static extern string nautilus_burn_glue_drive_get_device(IntPtr drive);

        public string Device {
            get { return nautilus_burn_glue_drive_get_device(Handle); }
        }
    }
}
