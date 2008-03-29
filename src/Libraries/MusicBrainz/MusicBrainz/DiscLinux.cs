/***************************************************************************
 *  DiscLinux.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

// This is based on $Id: disc_linux.c 8505 2006-09-30 00:02:18Z luks $

using System;
using System.Runtime.InteropServices;

namespace MusicBrainz
{
    internal sealed class DiscLinux : LocalDisc
    {
        const int O_RDONLY = 0x0;
        const int O_NONBLOCK = 0x4000;
        const int CDROMREADTOCHDR = 0x5305;
        const int CDROMREADTOCENTRY = 0x5306;
        const int CDROMMULTISESSION = 0x5310;
        const int CDROM_LBA = 0x01;
        const int CDROM_LEADOUT = 0xAA;
        const int CD_FRAMES = 75;
        const int XA_INTERVAL = ((60 + 90 + 2) * CD_FRAMES);
        
        [DllImport ("libc.so.6")]
        static extern int open (string path, int flags);
        
        [DllImport ("libc.so.6")]
        static extern int close (int fd);
        
        [DllImport ("libc.so.6", EntryPoint = "ioctl")]
        static extern int read_toc_header (int fd, int request, ref cdrom_tochdr header);
        static int read_toc_header (int fd, ref cdrom_tochdr header)
        {
            return read_toc_header (fd, CDROMREADTOCHDR, ref header);
        }
        
        [DllImport ("libc.so.6", EntryPoint = "ioctl")]
        static extern int read_multisession (int fd, int request, ref cdrom_multisession multisession);
        static int read_multisession (int fd, ref cdrom_multisession multisession)
        {
            return read_multisession (fd, CDROMMULTISESSION, ref multisession);
        }
        
        [DllImport ("libc.so.6", EntryPoint = "ioctl")]
        static extern int read_toc_entry (int fd, int request, ref cdrom_tocentry entry);
        static int read_toc_entry (int fd, ref cdrom_tocentry entry)
        {
            return read_toc_entry (fd, CDROMREADTOCENTRY, ref entry);
        }
        
        struct cdrom_tochdr
        {
            public byte cdth_trk0;
            public byte cdth_trk1;
        }
        
        struct cdrom_tocentry
        {
            public byte cdte_track;
            public byte adr_ctrl;
            public byte cdte_format;
            public int lba;
            public byte cdte_datamode;
        }
        
        struct cdrom_multisession
        {
            public int lba;
            public byte xa_flag;
            public byte addr_format;
        }
        
        int ReadTocHeader (int fd)
        {
            cdrom_tochdr th = new cdrom_tochdr ();
            cdrom_multisession ms = new cdrom_multisession ();
            
            int ret = read_toc_header (fd, ref th);
            
            if (ret < 0) return ret;
            
            FirstTrack = th.cdth_trk0;
            LastTrack = th.cdth_trk1;
            
            ms.addr_format = CDROM_LBA;
            ret = read_multisession (fd, ref ms);
            
            if(ms.xa_flag != 0) LastTrack--;
            
            return ret;
        }
        
        int ReadTocEntry (int fd, byte track_number, ref ulong lba)
        {
            cdrom_tocentry te = new cdrom_tocentry ();
            te.cdte_track = track_number;
            te.cdte_format = CDROM_LBA;
            
            int ret = read_toc_entry (fd, ref te);
            
            if(ret == 0) lba = (ulong)te.lba;
            
            return ret;
        }
        
        int ReadLeadout (int fd, ref ulong lba)
        {
            cdrom_multisession ms = new cdrom_multisession ();
            ms.addr_format = CDROM_LBA;
            
            int ret = read_multisession (fd, ref ms);
            
            if (ms.xa_flag != 0) {
                lba = (ulong)(ms.lba - XA_INTERVAL);
                return ret;
            }
            
            return ReadTocEntry (fd, CDROM_LEADOUT, ref lba);
        }
        
        internal DiscLinux (string device)
        {
            int fd = open (device, O_RDONLY | O_NONBLOCK);
            
            if (fd < 0) throw new Exception (String.Format ("Cannot open device '{0}'", device));
            
            try {
                if (ReadTocHeader(fd) < 0) throw new Exception ("Cannot read table of contents");
                if (LastTrack == 0) throw new Exception ("This disc has no tracks");
                
                ulong lba = 0;
                ReadLeadout (fd, ref lba);
                TrackOffsets [0] = (int)lba + 150;
                
                for (byte i = FirstTrack; i <= LastTrack; i++) {
                    ReadTocEntry (fd, i, ref lba);
                    TrackOffsets[i] = (int)lba + 150;
                }
            } finally {
                close (fd);
            }
            
            Init ();
        }
    }
}
