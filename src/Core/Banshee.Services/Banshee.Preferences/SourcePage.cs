//
// SourcePage.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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

using Banshee.ServiceStack;
using Banshee.Sources;

using Hyena.Data;

namespace Banshee.Preferences
{
    public class SourcePage : Page, IDisposable
    {
        private Source source;

        public SourcePage (Source source) : this (source.UniqueId, source.Name, null, source.Order)
        {
            this.source = source;
            source.Properties.PropertyChanged += OnPropertyChanged;
            UpdateIcon ();
        }

        public SourcePage (string uniqueId, string name, string iconName, int order) : base (uniqueId, name, order)
        {
            IconName = iconName;
            ServiceManager.Get<Banshee.Preferences.PreferenceService> ()["source-specific"].ChildPages.Add (this);
        }

        public void Dispose ()
        {
            ServiceManager.Get<Banshee.Preferences.PreferenceService> ()["source-specific"].ChildPages.Remove (this);
        }

        private void UpdateIcon ()
        {
            if (source.Properties.GetType ("Icon.Name") == typeof(string)) {
                IconName = source.Properties.Get<string> ("Icon.Name");
            } else if (source.Properties.GetType ("Icon.Name") == typeof(string[])) {
                IconName = source.Properties.Get<string[]> ("Icon.Name")[0];
            }
        }

        private void OnPropertyChanged (object o, PropertyChangeEventArgs args)
        {
            if (args.PropertyName == "Icon.Name") {
                UpdateIcon ();
            }
        }
    }
}
