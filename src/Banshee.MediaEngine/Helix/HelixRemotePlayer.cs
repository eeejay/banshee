/***************************************************************************
 *  HelixRemotePlayer.cs
 *
 *  Copyright (C) 2006 Novell, Inc
 *  Written by Aaron Bockover <aaron@abock.org>
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
using System.Collections.Generic;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace Helix
{    
    public delegate void MessageHandler(MessageType type, IDictionary<string, object> args);

    [Interface(RemotePlayer.Interface)]
    public interface IRemotePlayer
    {
        event MessageHandler Message;
        bool OpenUri(string uri);
        void Play();
        void Pause();
        void Stop();
        bool StartSeeking();
        void StopSeeking();
        bool SetPosition(uint position);
        uint GetPosition();
        uint GetLength();
        uint GetVolume();
        void SetVolume(uint volume);
        string GetGroupTitle(uint groupIndex);
        void Shutdown();
        void Ping();
        bool GetIsLive();
        bool GetIsEqualizerEnabled();
        void SetEqualizerEnabled(bool enabled);
        void SetEqualizerGain(int frequencyId, int value);
    }
    
    public static class RemotePlayer
    {
        public const string Interface = "org.gnome.HelixDbusPlayer";
        public const string ServiceName = "org.gnome.HelixDbusPlayer";
        public const string ObjectPath = "/org/gnome/HelixDbusPlayer/Player";
        
        private static IRemotePlayer instance;
        
        public static IRemotePlayer Connect()
        {
            if(instance != null) {
                return instance;
            }
            
            Connection connection = DApplication.Connection;
            Bus bus = connection.GetObject<Bus>("org.freedesktop.DBus", 
                new ObjectPath("/org/freedesktop/DBus"));
                
            if(bus.StartServiceByName(ServiceName, 0) == StartReply.Success) {
                Console.WriteLine("Started {0}", ServiceName);
            } else {
                Console.WriteLine("{0} was already started", ServiceName);
            }
            
            if(!bus.NameHasOwner(Interface)) {
                return null;
            }
            
            instance = connection.GetObject<IRemotePlayer>(Interface, new ObjectPath(ObjectPath));
            return instance;
        }
    }
    
    public enum ContentState {
        NotLoaded = 0,
        Contacting,
        Loading,
        Stopped,
        Playing,
        Paused
    };
    
    public enum MessageType {
        None = 0,
        VisualState,
        IdealSize,
        Length,
        Title,
        Groups,
        GroupStarted,
        Contacting,
        Buffering,
        ContentConcluded,
        ContentState,
        Status,
        Volume,
        Mute,
        ClipBandwidth,
        Error,
        GotoUrl,
        RequestAuthentication,
        RequestUpgrade,
        HasComponent
    }
}
