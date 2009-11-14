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
    public abstract class PreferenceBase : Root
    {
        public event Action<Root> ValueChanged;

        public PreferenceBase ()
        {
        }

        public abstract object BoxedValue { get; set; }

        private bool show_label = true;
        public virtual bool ShowLabel {
            get { return show_label; }
            set { show_label = value; }
        }

        private bool show_description = false;
        public virtual bool ShowDescription {
            get { return show_description; }
            set { show_description = value; }
        }

        protected void OnValueChanged ()
        {
            Action<Root> handler = ValueChanged;
            if (handler != null) {
                handler (this);
            }
        }
    }
}
