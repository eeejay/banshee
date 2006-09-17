using System;

namespace Banshee.Cdrom.Nautilus.Interop
{
	// For NautilusBurnRecorder

    internal enum BurnRecorderResult {
    	#if HAVE_LNB_216
        	Error = -1,
        	Cancel = -2,
        	Finished = -3,
        	Retry = -4
   		#else 
            Error,
            Cancel,
            Finished,
            Retry
    	#endif
    }
    
    internal enum BurnRecorderResponse {
        None,
        Cancel = -1,
        Erase = -2,
        Retry = -3
    }
    
    public enum BurnRecorderTrackType {
        Unknown,
        Audio,
        Data,
        Cue,
        GraftList
    }

    [Flags]
    internal enum BurnRecorderWriteFlags {
        None,
        Eject = 1 << 0,
        Blank = 1 << 1,
        DummyWrite = 1 << 2,
        DiscAtOnce = 1 << 3,
        Debug = 1 << 4,
        Overburn = 1 << 5,
        Burnproof = 1 << 6,
        Joliet = 1 << 7
    }
    
    [Flags]
    internal enum BurnRecorderBlankFlags {
        None,
        DummyWrite = 1 << 1,
        Debug = 1 << 2
    }

    internal enum BurnRecorderBlankType {
        Fast,
        Full
    }
        internal enum BurnRecorderActions {
        PreparingWrite,
        Writing,
        Fixating,
        Blanking
    }
    
    internal enum BurnRecorderMedia {
        Cd,
        Dvd
    }

	// For NautilusBurnDrive
	
    internal enum BurnMediaType {
        Busy,
        Error,
        Unknown,
        Cd,
        Cdr,
        Cdrw,
        Dvd,
        Dvdr,
        Dvdrw,
        DvdRam,
        DvdPlusR,
        DvdPlusRw,
        DvdPlusRDl
    }
}
