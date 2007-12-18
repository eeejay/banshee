//
// ErrorSource.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using Mono.Unix;

using Hyena.Data;
using Hyena.Collections;

namespace Banshee.Sources
{
    public class ErrorSource : Source, IObjectListModel
    {
        private List<Message> messages = new List<Message> ();
        private Selection selection = new Selection ();
        
        public event EventHandler Cleared;
        public event EventHandler Reloaded;

        public ErrorSource (string name) : base (name, name, 0)
        {
            Properties.SetStringList ("IconName", "dialog-error", "gtk-dialog-error");
        }
        
        private void OnReloaded ()
        {
            EventHandler handler = Reloaded;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        private void OnCleared ()
        {
            EventHandler handler = Cleared;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
        
        private ColumnDescription [] columns = new ColumnDescription [] {
            new ColumnDescription ("Title", Catalog.GetString ("Error"), .35),
            new ColumnDescription ("Details", Catalog.GetString ("Details"), .65)
        };
        
        public ColumnDescription [] ColumnDescriptions {
            get { return columns; }
        }
        
        public override void Activate ()
        {
            Reload ();
        }
        
        public void AddMessage (string title, string details)
        {
           AddMessage (new Message (title, details));
        }
        
        public void AddMessage (Message message)
        {
            lock (this) {
                messages.Add (message);
            }
            
            OnUpdated ();
            OnReloaded ();
        }
        
        public void Clear ()
        {
            lock (this) {
                messages.Clear ();
            }
            
            OnUpdated ();
            OnCleared ();
        }
        
        public void Reload ()
        {
            OnReloaded ();
        }

        public override int Count {
            get { return messages.Count; }
        }
        
        public override bool CanSearch {
            get { return false; }
        }
        
        public object this[int index] {
            get {
                if (index >= 0 && index < messages.Count) {
                    return messages[index];
                } 
                
                return null;
            }
        }

        public Selection Selection {
            get { return selection; }
        }
        
        public class Message
        {
            private string title;
            private string details;
            
            public Message (string title, string details)
            {
                this.title = title;
                this.details = details;
            }
            
            public string Title {
                get { return title; }
            }
            
            public string Details {
                get { return details; }
            }
        }
    }
}
