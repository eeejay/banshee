/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  VLC.cs
 *
 *  Copyright (C) 2005 Jon Lech Johansen (jon@nanocrew.net)
 *  Written by Jon Lech Johansen, Aaron Bockover (aaron@aaronbock.net)
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
using System.Runtime.InteropServices;

public class VLC : IDisposable
{
    public enum Error {
        Success = -0,
        NoMem   = -1,
        Thread  = -2,
        Timeout = -3,

        NoMod   = -10,

        NoObj   = -20,
        BadObj  = -21,

        NoVar   = -30,
        BadVar  = -31,

        Exit    = -255,
        Generic = -666
    }

    private enum Mode {
        Insert      = 0x01,
        Replace     = 0x02,
        Append      = 0x04,
        Go          = 0x08,
        CheckInsert = 0x10
    }

    private enum Pos {
        End = -666
    };

    [DllImport("libvlc")]
    private static extern int VLC_Create();
    
    [DllImport("libvlc")]
    private static extern Error VLC_Init(int iVLC, int argc, string [] argv);
    
    [DllImport("libvlc")]
    private static extern Error VLC_AddIntf(int iVLC, string name, 
        bool block, bool play);
    
    [DllImport("libvlc")]
    private static extern Error VLC_Die(int iVLC);
    
    [DllImport("libvlc")]
    private static extern Error VLC_CleanUp(int iVLC);
    
    [DllImport("libvlc")]
    private static extern Error VLC_Destroy(int iVLC);
    
    [DllImport("libvlc")]
    private static extern Error VLC_AddTarget(int iVLC, string target,
        string [] options, int options_count, int mode, int position);
        
    [DllImport("libvlc")]
    private static extern Error VLC_PlaylistClear(int iVLC);
        
    [DllImport("libvlc")]
    private static extern Error VLC_Play(int iVLC);
    
    [DllImport("libvlc")]
    private static extern Error VLC_Pause(int iVLC );
    
    [DllImport("libvlc")]
    private static extern Error VLC_Stop(int iVLC );
    
    [DllImport("libvlc")]
    private static extern bool VLC_IsPlaying(int iVLC);
    
    [DllImport("libvlc")]
    private static extern int VLC_TimeGet(int iVLC);
    
    [DllImport("libvlc")]
    private static extern Error VLC_TimeSet(int iVLC, int seconds, bool relative);
    
    [DllImport("libvlc")]
    private static extern int VLC_LengthGet(int iVLC);
    
    [DllImport("libvlc")]
    private static extern int VLC_VolumeSet(int iVLC, int volume);
    
    [DllImport("libvlc")]
    private static extern int VLC_VolumeGet(int iVLC);
    
    private int iVLC;

    public VLC()
    {
        iVLC = VLC_Create();
        
        if(iVLC < 0) {
            throw new ApplicationException("VLC_Create failed");
        }
            
        Error err = VLC_Init(iVLC, 2, new string[] { "vlc", "--quiet" });
        if(err != Error.Success) {
            VLC_Destroy(iVLC);
            throw new ApplicationException("VLC_Init failed");
        }
    }

    public void Dispose()
    {
        VLC_CleanUp(iVLC);
        VLC_Destroy(iVLC);
    }
    
    public bool Open(string target)
    {
        VLC_PlaylistClear(iVLC);
        return VLC_AddTarget(iVLC, target, null, 0, (int)Mode.Replace, 0) > 0;
    }
    
    public Error Play()
    {
        return VLC_Play(iVLC);
    }

    public Error Pause()
    {
        return VLC_Pause(iVLC);
    }

    public Error Stop()
    {
        return VLC_Stop(iVLC);
    }

    public int Time
    {
        get {
            return VLC_TimeGet(iVLC);
        }
        
        set {
            VLC_TimeSet(iVLC, value, false);
        }
    }
    
    public int Length
    {
        get {
            return VLC_LengthGet(iVLC);
        }
    }
    
    public bool IsPlaying
    {
        get {
            return VLC_IsPlaying(iVLC);
        }
    }
    
    public int Volume
    {
        get {
            return VLC_VolumeGet(iVLC);
        }
        
        set {
            VLC_VolumeSet(iVLC, value);
        }
    }
}
