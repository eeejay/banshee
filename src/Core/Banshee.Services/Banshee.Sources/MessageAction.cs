//
// MessageAction.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

namespace Banshee.Sources
{
    public class MessageAction
    {
        private bool is_stock;
        private string label;

        public event EventHandler Activated;

        public MessageAction (string label) : this (label, false, null)
        {
        }

        public MessageAction (string label, EventHandler handler) : this (label, false, handler)
        {
        }

        public MessageAction (string label, bool isStock) : this (label, isStock, null)
        {
        }

        public MessageAction (string label, bool isStock, EventHandler handler)
        {
            this.label = label;
            this.is_stock = isStock;
            if (handler != null) {
                this.Activated += handler;
            }
        }

        public void Activate ()
        {
            OnActivated ();
        }

        protected virtual void OnActivated ()
        {
            EventHandler handler = Activated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        public bool IsStock {
            get { return is_stock; }
            set { is_stock = value; }
        }

        public string Label {
            get { return label; }
            set { label = value; }
        }
    }
}
