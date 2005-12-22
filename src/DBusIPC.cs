/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  DBusIPC.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using DBus;
using Banshee.Base;

namespace Banshee
{
    public class DBusServer
    {
        private Service service;
        
        private static DBusServer instance = null;
        public static DBusServer Instance {
            get {
                if(instance == null) {
                    instance = new DBusServer();
                }
                
                return instance;
            }
        }
        
        public DBusServer()
        {
            try {
                service = new Service(Bus.GetSessionBus(), "org.gnome.Banshee");
            } catch(Exception e) {
                LogCore.Instance.PushWarning("Could not connect to D-Bus", 
                    "D-Bus support will be disabled for this instance: " + e.Message,
                    false);
            }
        }
        
        public void RegisterObject(object o, string path)
        {
            if(service != null) {
                service.RegisterObject(o, path);
            }
        }
        
        public void UnregisterObject(object o)
        {
            if(service != null) {
                service.UnregisterObject(o);
            }
        }
    }
    
    [Interface("org.gnome.Banshee.Core")]
    public class BansheeCore
    {
        private Gtk.Window mainWindow;
        private PlayerUI PlayerUI;
        public static BansheeCore FindInstance()
        {
            Connection connection = Bus.GetSessionBus();
            Service service = Service.Get(connection, "org.gnome.Banshee");        
            return (BansheeCore)service.GetObject(typeof(BansheeCore), "/org/gnome/Banshee/Core");
        }
        
        public BansheeCore(Gtk.Window mainWindow, PlayerUI ui)
        {
            this.mainWindow = mainWindow;
            this.PlayerUI = ui;
        }
        
        [Method]
        public virtual void PresentWindow()
        {
            if(mainWindow != null) {
                mainWindow.Present();
            }
        }
        
        [Method]
        public virtual void ShowWindow()
        {
            if(mainWindow != null) {
                mainWindow.Show();
            }
        }
        
        [Method]
        public virtual void HideWindow()
        {
            if(mainWindow != null) {
                mainWindow.Hide();
            }
        }
        
        [Method]
        public virtual void TogglePlaying()
        {
            if(PlayerUI != null) {
                PlayerUI.PlayPause();
            }
        }
        
        [Method]
        public virtual void Play()
        {
            if(PlayerUI == null) {
                return;
            }
            
            if(PlayerUI != null && !HaveTrack) {
                PlayerUI.PlayPause();
            }
            
            if(!PlayerEngineCore.ActivePlayer.Playing) {
                PlayerUI.PlayPause();
            }
        }
        
        [Method]
        public virtual void Pause()
        {
            if(HaveTrack && PlayerEngineCore.ActivePlayer.Playing) {
                PlayerUI.PlayPause();
            }
        }
        
        [Method]
        public virtual void Next()
        {
            if(PlayerUI != null) {
                PlayerUI.Next();
            }
        }
        
        [Method]
        public virtual void Previous()
        {
            if(PlayerUI != null) {
                PlayerUI.Previous();
            }
        }

        [Method]
        public virtual void SelectAudioCd(string device)
        {
            if(PlayerUI != null) {
                PlayerUI.SelectAudioCd(device);
            }
        }
        
        [Method]
        public virtual void SelectDap(string device)
        {
            if(PlayerUI != null) {
                PlayerUI.SelectDap(device);
            }
        }
        
        private bool HaveTrack {
            get {
                return PlayerUI != null && PlayerUI.ActiveTrackInfo != null;
            }
        }
        
        [Method]
        public virtual string GetPlayingArtist()
        {
            return HaveTrack ? PlayerUI.ActiveTrackInfo.Artist : String.Empty;
        }
        
        [Method]
        public virtual string GetPlayingAlbum()
        {
            return HaveTrack ? PlayerUI.ActiveTrackInfo.Album : String.Empty;
        }
        
        [Method]
        public virtual string GetPlayingTitle()
        {
            return HaveTrack ? PlayerUI.ActiveTrackInfo.Title : String.Empty;
        }
        
        [Method]
        public virtual string GetPlayingGenre()
        {
            return HaveTrack ? PlayerUI.ActiveTrackInfo.Genre : String.Empty;
        }
        
        [Method]
        public virtual string GetPlayingUri()
        {
            return HaveTrack ? PlayerUI.ActiveTrackInfo.Uri.AbsoluteUri : String.Empty;
        }
        
        [Method]
        public virtual int GetPlayingDuration()
        {
            return HaveTrack ? (int)PlayerEngineCore.ActivePlayer.Length : -1;
        }
        
        [Method]
        public virtual int GetPlayingPosition()
        {
            return HaveTrack ? (int)PlayerEngineCore.ActivePlayer.Position : -1;
        }
        
        [Method]
        public virtual string GetPlayingCoverArtFileName()
        {
            return HaveTrack ? PlayerUI.ActiveTrackInfo.CoverArtFileName : String.Empty;
        }
        
        [Method]
        public virtual int GetPlayingStatus()
        {
            return PlayerEngineCore.ActivePlayer.Playing ? 1 : (PlayerEngineCore.ActivePlayer.Loaded ? 0 : -1);
        }
        
        [Method]
        public virtual void SetVolume(int volume)
        {
            if(PlayerUI != null) {
                PlayerUI.Volume = volume;
            }
        }

        [Method]
        public virtual void IncreaseVolume()
        {
            if(PlayerUI != null) {
                PlayerUI.Volume += PlayerUI.VolumeDelta;
            }
        }
        
        [Method]
        public virtual void DecreaseVolume()
        {
            if(PlayerUI != null) {
                PlayerUI.Volume -= PlayerUI.VolumeDelta;
            }
        }
        
        [Method]
        public virtual void SetPlayingPosition(int position)
        {
            PlayerEngineCore.ActivePlayer.Position = (uint)position;
        }
        
        [Method]
        public virtual void SkipForward()
        {
            PlayerEngineCore.ActivePlayer.Position += PlayerUI.SkipDelta;
        }
        
        [Method]
        public virtual void SkipBackward()
        {
            PlayerEngineCore.ActivePlayer.Position -= PlayerUI.SkipDelta;
        }
    }
}

