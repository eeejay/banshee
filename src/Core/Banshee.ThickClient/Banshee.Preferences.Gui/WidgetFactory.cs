//
// WidgetFactory.cs
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
using System.Reflection;
using Gtk;

using Banshee.Preferences;

namespace Banshee.Preferences.Gui
{
    public static class WidgetFactory
    {
        public static Widget GetWidget (PreferenceBase preference)
        {
            if (preference == null) {
                return null;
            }
            
            Widget display_widget = preference.DisplayWidget as Widget;
            
            return display_widget ?? GetWidget (preference, preference.GetType ().GetProperty ("Value").PropertyType);
        }
        
        private static Widget GetWidget (PreferenceBase preference, Type type)
        {
            if (type == typeof (bool)) {
                return new PreferenceCheckButton (preference);
            } else if (type == typeof (string)) {
                return new PreferenceEntry (preference);
            }
            
            return null;
        }
        
        private class PreferenceCheckButton : CheckButton
        {
            private bool sync;
            private PreferenceBase preference;
            
            public PreferenceCheckButton (PreferenceBase preference)
            {
                this.preference = preference;
                Label = preference.Name;
                UseUnderline = true;
                Active = (bool)preference.BoxedValue;
                sync = true;
            }
            
            protected override void OnToggled ()
            {
                base.OnToggled ();
                
                if (sync) {
                    preference.BoxedValue = Active;
                }
            }
        }
        
        private class PreferenceEntry : Entry
        {
            private bool sync;
            private PreferenceBase preference;
            
            public PreferenceEntry (PreferenceBase preference)
            {
                this.preference = preference;
                string value = (string)preference.BoxedValue;
                Text = value == null ? String.Empty : value;
                sync = true;
            }
            
            protected override void OnChanged ()
            {
                base.OnChanged ();
                
                if (sync) {
                    preference.BoxedValue = Text;
                }
            }
        }
    }
}
