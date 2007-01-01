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

namespace Helix
{    
    public delegate void MessageHandler(MessageType type, IDictionary<string, object> args);

    [Interface(RemotePlayer.Interface)]
    public interface IRemotePlayer
    {
        event MessageHandler Message;
        
        void Shutdown();
        void Ping();
        
        bool OpenUri(string uri);
        
        void Play();
        void Pause();
        void Stop();
        
        bool SetPosition(uint position);
        bool StartSeeking();
        void StopSeeking();
        
        string GetGroupTitle(uint groupIndex);
        
        void SetEqualizerGain(int frequencyId, int value);
        
        uint Position { get; }
        uint Length { get; }
        uint Volume { get; set; }
        bool IsLive { get; }
        bool IsEqualizerEnabled { get; set; }
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
    
    public static class RemotePlayer
    {
        public const string Interface = "org.gnome.HelixDbusPlayer";
        public const string ServiceName = "org.gnome.HelixDbusPlayer";
        public const string ObjectPath = "/org/gnome/HelixDbusPlayer/Player";

        public static IRemotePlayer Connect()
        {
            Bus.Session.StartServiceByName(ServiceName);
            
            if(!Bus.Session.NameHasOwner(Interface)) {
                return null;
            }
            
            return Bus.Session.GetObject<IRemotePlayer>(
                Interface, new ObjectPath(ObjectPath));
        }
    }
}
