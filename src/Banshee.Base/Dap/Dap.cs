/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Dap.cs
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
using System.Collections;

using Banshee.Base;

namespace Banshee.Dap
{
    public enum DapType {
        Generic,
        NonGeneric
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DapProperties : Attribute 
    {
        private DapType dap_type;
        
        public DapType DapType {
            get {
                return dap_type;
            }
            
            set {
                dap_type = value;
            }
        }
    }
    
    public class BrokenDeviceException : ApplicationException
    {
        public BrokenDeviceException(string message) : base(message)
        {
        }
    }
    
    
    public class CannotHandleDeviceException : ApplicationException
    {
        public CannotHandleDeviceException() : base("HAL Device cannot be handled by Dap subclass")
        {
        }
    }
    
    public class WaitForPropertyChangeException : ApplicationException
    {
        public WaitForPropertyChangeException() : base("Waiting for properties to change on device")
        {
        }
    }
    
    public delegate void DapTrackListUpdatedHandler(object o, DapTrackListUpdatedArgs args);

    public class DapTrackListUpdatedArgs : EventArgs
    {
        public TrackInfo Track;

        public DapTrackListUpdatedArgs(TrackInfo track)
        {
            Track = track;
        }
    }
    
    public abstract class DapDevice : IEnumerable
    {
        public class PropertyTable : IEnumerable
        {
            public class Property
            {
                public string Name;
                public string Value;
            }
        
            private ArrayList properties = new ArrayList();
            
            private Property Find(string name)
            {
                foreach(Property property in properties) {
                    if(property.Name == name) {
                        return property;
                    }
                }
                
                return null;
            }
            
            public void Add(string name, string value)
            {
                if(value == null || value.Trim() == String.Empty) {
                    return;
                }
                
                Property property = Find(name);
                if(property != null) {
                    property.Value = value;
                    return;
                } 
                
                property = new Property();
                property.Name = name;
                property.Value = value;
            
                properties.Add(property);
            }
            
            public string this [string name] {
                get {
                    Property property = Find(name);
                    return property == null ? null : property.Value;
                }
            }
            
            public IEnumerator GetEnumerator()
            {
                foreach(Property property in properties) {
                    yield return property.Name;
                }
            }
        }

        private PropertyTable properties = new PropertyTable();
        private ArrayList tracks = new ArrayList(); 
        
        public event DapTrackListUpdatedHandler TrackAdded;
        public event DapTrackListUpdatedHandler TrackRemoved;
        public event EventHandler TracksCleared;
        public event EventHandler PropertiesChanged;
        
        public IEnumerator GetEnumerator()
        {
            return tracks.GetEnumerator();
        }
        
        protected void InvokePropertiesChanged()
        {
            if(PropertiesChanged != null) {
                PropertiesChanged(this, new EventArgs());
            }
        }
        
        protected void InstallProperty(string name, string value)
        {
            properties.Add(name, value);
            InvokePropertiesChanged();
        }
        
        public void AddTrack(TrackInfo track)
        {
            tracks.Add(track);
            OnTrackAdded(track);
            Event.Invoke(TrackAdded, this, delegate { return new DapTrackListUpdatedArgs(track); });
        }
        
        public void RemoveTrack(TrackInfo track)
        {
            tracks.Remove(track);
            OnTrackRemoved(track);
            Event.Invoke(TrackRemoved, this, delegate { return new DapTrackListUpdatedArgs(track); });
        }
        
        public void ClearTracks()
        {
            ClearTracks(true);
        }
        
        protected void ClearTracks(bool notify)
        {
            tracks.Clear();
            if(notify) {
                OnTracksCleared();
                Event.Invoke(TracksCleared, this);
            }
        }
        
        protected virtual void OnTrackAdded(TrackInfo track)
        {
        }
        
        protected virtual void OnTrackRemoved(TrackInfo track)
        {
        }
        
        protected virtual void OnTracksCleared()
        {
        }
        
        public virtual void Eject()
        {
        }
        
        public virtual void Save()
        {
        }
        
        public virtual Gdk.Pixbuf GetIcon(int size)
        {
            return null;
        }
        
        public PropertyTable Properties {
            get {
                return properties;
            }
        }
        
        public TrackInfo this [int index] {
            get {
                return tracks[index] as TrackInfo;
            }
        }
        
        public int TrackCount { 
            get {
                return tracks.Count;
            }
        }
        
        public IList Tracks {
            get {
                return tracks;
            }
        }
        
        public abstract string Name { get; set; }
        public abstract ulong StorageCapacity { get; }
        public abstract ulong StorageUsed { get; }
        public abstract bool IsReadOnly { get; }
        public abstract bool IsPlaybackSupported { get; }
    }
}
