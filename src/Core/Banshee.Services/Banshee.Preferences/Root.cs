//
// PreferenceBase.cs
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

namespace Banshee.Preferences
{
    public abstract class Root : IComparable
    {
        private string id;
        private string name;
        private string description;
        private int order;
        private bool sensitive;
        private bool visible;
        private object display_widget;
        private object mnemonic_widget;

        public event Action<Root> Changed;

        public Root ()
        {
            sensitive = true;
            visible = true;
        }

        public int CompareTo (object o)
        {
            Root r = o as Root;
            if (r == null) {
                return -1;
            }

            return Order.CompareTo (r.Order);
        }

        public string Id {
            get { return id; }
            set { id = value; }
        }

        public string Name {
            get { return name; }
            set {
                name = value;
                OnChanged ();
            }
        }

        public string Description {
            get { return description; }
            set {
                description = value;
                OnChanged ();
            }
        }

        public int Order {
            get { return order; }
            set { order = value; }
        }

        public virtual bool Sensitive {
            get { return sensitive; }
            set {
                sensitive = value;
                OnChanged ();
            }
        }

        public virtual bool Visible {
            get { return visible; }
            set {
                visible = value;
                OnChanged ();
            }
        }

        public virtual object DisplayWidget {
            get { return display_widget; }
            set { display_widget = value; }
        }

        public virtual object MnemonicWidget {
            get { return mnemonic_widget ?? DisplayWidget; }
            set { mnemonic_widget = value; }
        }

        protected void OnChanged ()
        {
            Action<Root> handler = Changed;
            if (handler != null) {
                handler (this);
            }
        }
    }
}
