using System;
using System.Runtime.InteropServices;
using Mono.Unix;

namespace Hal
{
    public class DBusError : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct UnmanagedError
        {
            public IntPtr name;
            public IntPtr message;
            public uint dummy1;
            public uint dummy2;
            public uint dummy3;
            public uint dummy4;
            public uint dummy5;
            public IntPtr padding1;        
        }
        
        [DllImport("libdbus-1")]
        private static extern void dbus_error_init(IntPtr handle);
        
        [DllImport("libdbus-1")]
        private static extern void dbus_error_free(IntPtr handle);
        
        [DllImport("libdbus-1")]
        private static extern bool dbus_error_is_set(IntPtr handle);
        
        private IntPtr error_ptr;
        private UnmanagedError error;

        public DBusError()
        {
            error = new UnmanagedError();
            error_ptr = Marshal.AllocHGlobal(Marshal.SizeOf(error));
            dbus_error_init(error_ptr);
        }
        
        public void Dispose()
        {
            dbus_error_free(error_ptr);
            Marshal.FreeHGlobal(error_ptr);
        }
        
        public void ThrowExceptionIfSet(string message)
        {
            if(IsSet) {
                message += String.Format(": [{0}] {1}", Name, Message);
                Dispose();
                throw new HalException(message);
            } else {
                Dispose();
            }
        }
        
        public bool IsSet {
            get {
                return dbus_error_is_set(error_ptr);
            }
        }
        
        public string Name {
            get {
                return UnixMarshal.PtrToString(error.name);
            }
        }
        
        public string Message {
            get {
                return UnixMarshal.PtrToString(error.message);
            }
        }
        
        public IntPtr Raw {
            get {
                return error_ptr;
            }
        }
    }
}
