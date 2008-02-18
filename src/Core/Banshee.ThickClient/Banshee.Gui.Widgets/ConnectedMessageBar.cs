//
// ConnectedMessageBar.cs
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
using Gtk;

using Hyena.Data;
using Banshee.Sources;
using Banshee.ServiceStack;

using Hyena.Widgets;
using Banshee.Sources.Gui;

namespace Banshee.Gui.Widgets
{
    public class ConnectedMessageBar : MessageBar
    {
        private Source source;
        
        public ConnectedMessageBar ()
        {
            ButtonClicked += OnActionClicked;
            CloseClicked += OnCloseClicked;
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;
            ConnectSource (ServiceManager.SourceManager.ActiveSource);
        }
        
        private void ConnectSource (Source source)
        {
            this.source = source;
            
            if (this.source != null) {
                this.source.Properties.PropertyChanged += OnSourcePropertyChanged;
                
                UpdateText (this.source.Properties.GetString ("Message.Text"), false);
                UpdateClose (this.source.Properties.GetBoolean ("Message.CanClose"), false);
                UpdateAction (false);
                UpdateIcon (false);
                UpdateSpinner (this.source.Properties.GetBoolean ("Message.IsSpinning"), false);
            }
        }
        
        private void UpdateText (object value, bool removed)
        {
            string message = removed || value == null ? null : value.ToString ();
            Message = message;
            
            if (message == null || removed || source.Properties.GetBoolean ("Message.IsHidden")) {
                Hide ();
            } else {
                Show ();
            }
        }
        
        private void UpdateIcon (bool removed)
        {
            if (removed) {
                SourceIconResolver.InvalidatePixbufs (source, "Message");
                Pixbuf = null;
                return;
            }
            
            Pixbuf = SourceIconResolver.ResolveIcon (source, "Message");
        }
        
        private void UpdateClose (bool value, bool removed)
        {
            ShowCloseButton = value && !removed; 
        }
        
        private void UpdateAction (bool removed)
        {
            if (removed) {
                ButtonLabel = null; 
                return;
            }
            
            ButtonLabel = source.Properties.GetString ("Message.Action.Label");
            ButtonUseStock = source.Properties.GetBoolean ("Message.Action.IsStock");
        }
        
        private void UpdateSpinner (bool value, bool removed)
        {
            Spinning = value && !removed; 
        }
        
        private void OnActionClicked (object o, EventArgs args)
        {
            EventHandler handler = source.Properties.Get<EventHandler> ("Message.Action.NotifyHandler");
            if (handler != null) {
                handler (o, args);
            }
        }
        
        private void OnCloseClicked (object o, EventArgs args)
        {
            source.Properties.SetBoolean ("Message.IsHidden", true);
        }
        
        private void OnActiveSourceChanged (SourceEventArgs args)
        {
            if (source != null && source != args.Source) {
                source.Properties.PropertyChanged -= OnSourcePropertyChanged;
            }
            
            ConnectSource (args.Source);
        }
        
        private void OnSourcePropertyChanged (object o, PropertyChangeEventArgs args)
        {
            if (!args.PropertyName.StartsWith ("Message.")) {
                return;
            }
            
            if (args.PropertyName == "Message.Text") {
                UpdateText (args.NewValue, args.Removed);
            } else if (args.PropertyName == "Message.Icon.Name") {
                SourceIconResolver.InvalidatePixbufs (source, "Message");
                UpdateIcon (args.Removed);
            } else if (args.PropertyName == "Message.CanClose") {
                UpdateClose (args.NewValue == null ? false : (bool)args.NewValue, args.Removed);
            } else if (args.PropertyName.StartsWith ("Message.Action.")) {
                UpdateAction (args.Removed);
            } else if (args.PropertyName == "Message.IsSpinning") {
                UpdateSpinner (args.NewValue == null ? false : (bool)args.NewValue, args.Removed);
            } else if (args.PropertyName == "Message.IsHidden") {
                bool value = args.NewValue == null ? false : (bool)args.NewValue;
                if (args.Removed || !value) {
                    UpdateText (this.source.Properties.GetString ("Message.Text"), false);
                } else if (value) {
                    Hide ();
                }
            }
        }
    }
}
