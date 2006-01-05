/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Source.cs
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

namespace Banshee.Sources
{
    public abstract class Source
    {
        private int order;
        private string name;

        public event EventHandler Updated;
        
        protected Source(string name, int order)
        {
            this.name = name;
            this.order = order;
        }
        
        public string Name {
            get {
                return name;
            }
            
            protected set {
                name = value;
            }
        }
        
        public bool Rename(string newName)
        {
            if(!UpdateName(name, newName)) {
                return false;
            }
                    
            OnUpdated();
            
            return true;
        }
        
        protected virtual bool UpdateName(string oldName, string newName)
        {
            return false;
        }
        
        public virtual bool Eject()
        {
            return false;
        }
        
        public virtual void ShowPropertiesDialog()
        {
        }

        public virtual void AddTrack(TrackInfo track)
        {
        }
        
        public void AddTrack(ICollection tracks)
        {
            foreach(TrackInfo track in tracks) {
                AddTrack(track);
            }
        }
        
        public virtual void RemoveTrack(TrackInfo track)
        {
        }
        
        public virtual void Commit()
        {
        }
        
        public virtual void Reorder(TrackInfo track, int position)
        {
        }
        
        protected virtual void OnUpdated()
        {
            EventHandler handler = Updated;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        public abstract int Count {
            get;
        }
        
        public virtual ICollection Tracks {
            get {
                return new ArrayList();
            }
        }
        
        public virtual Gdk.Pixbuf Icon {
            get {
                return null;
            }
        }
        
        public int Order {
            get {
                return order;
            }
        }
        
        public bool CanEject {
            get {
                return ReflectionUtil.IsVirtualMethodImplemented(GetType(), "Eject");
            }
        }
        
        private bool can_rename = true;
        public bool CanRename {
            get {
                return ReflectionUtil.IsVirtualMethodImplemented(GetType(), "UpdateName") && can_rename;
            }
            
            protected set {
                can_rename = value;
            }
        }
        
        public bool HasProperties {
            get {
                return ReflectionUtil.IsVirtualMethodImplemented(GetType(), "ShowPropertiesDialog");
            }
        }
        
        public bool CanRemoveTracks {
            get {
                return ReflectionUtil.IsVirtualMethodImplemented(GetType(), "RemoveTrack");
            }
        }
    }
}
