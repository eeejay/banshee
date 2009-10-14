// 
// PanelClient.cs
//  
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2009 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.InteropServices;

namespace Mutter
{
    public class PanelClient : GLib.Object
    {
        [DllImport ("libmoblin-panel-gtk")]
        private static extern IntPtr mpl_panel_client_get_type ();
    
        public static new GLib.GType GType {
            get { return new GLib.GType (mpl_panel_client_get_type ()); }
        }
        
        protected PanelClient () : base (IntPtr.Zero)
        {
            CreateNativeObject (new string [0], new GLib.Value [0]);
        }
    
        public PanelClient (IntPtr raw) : base (raw)
        {
        }
    
        [DllImport ("libmoblin-panel-gtk")]
        private static extern void mpl_panel_client_request_show (IntPtr panel);
    
        public void RequestShow ()
        {
            mpl_panel_client_request_show (Handle);
        }
    
        [DllImport ("libmoblin-panel-gtk")]
        private static extern void mpl_panel_client_request_hide (IntPtr panel);
    
        public void RequestHide ()
        {
            mpl_panel_client_request_hide (Handle);
        }
    
        [DllImport ("libmoblin-panel-gtk")]
        private static extern void mpl_panel_client_request_focus (IntPtr panel);
    
        public void RequestFocus ()
        {
            mpl_panel_client_request_focus (Handle);
        }
    
        [DllImport ("libmoblin-panel-gtk")]
        private static extern void mpl_panel_client_set_height_request (IntPtr panel, uint height);
        
        [DllImport ("libmoblin-panel-gtk")]
        private static extern uint mpl_panel_client_get_height_request (IntPtr panel);
    
        public uint HeightRequest {
            get { return mpl_panel_client_get_height_request (Handle); }
            set { mpl_panel_client_set_height_request (Handle, value); }
        }
        
        [DllImport ("libmoblin-panel-gtk")]
        private static extern void mpl_panel_client_request_button_style (IntPtr panel, string style);
    
        public string ButtonStyleRequest {
            set { mpl_panel_client_request_button_style (Handle, value); }
        }
    
        [DllImport ("libmoblin-panel-gtk")]
        private static extern void mpl_panel_client_request_tooltip (IntPtr panel, string tooltip);
    
        public string TooltipRequest {
            set { mpl_panel_client_request_tooltip (Handle, value); }
        }
    
        [DllImport ("libmoblin-panel-gtk")]
        private static extern uint mpl_panel_client_get_xid (IntPtr panel);
    
        public uint Xid {
            get { return mpl_panel_client_get_xid (Handle); }
        }
        
        [GLib.Signal ("set-size")]
        public event SetSizeHandler SetSizeEvent {
            add { GLib.Signal.Lookup (this, "set-size", typeof (SetSizeArgs)).AddDelegate (value); }
            remove { GLib.Signal.Lookup (this, "set-size", typeof (SetSizeArgs)).RemoveDelegate (value); }
        }
        
        [GLib.Signal ("show-begin")]
        public event EventHandler ShowBeginEvent {
            add { GLib.Signal.Lookup (this, "show-begin").AddDelegate (value); }
            remove { GLib.Signal.Lookup (this, "show-begin").RemoveDelegate (value); }
        }
        
        [GLib.Signal ("show-end")]
        public event EventHandler ShowEndEvent {
            add { GLib.Signal.Lookup (this, "show-end").AddDelegate (value); }
            remove { GLib.Signal.Lookup (this, "show-end").RemoveDelegate (value); }
        }
        
        [GLib.Signal ("hide-begin")]
        public event EventHandler HideBeginEvent {
            add { GLib.Signal.Lookup (this, "hide-begin").AddDelegate (value); }
            remove { GLib.Signal.Lookup (this, "hide-begin").RemoveDelegate (value); }
        }
        
        [GLib.Signal ("hide-end")]
        public event EventHandler HideEndEvent {
            add { GLib.Signal.Lookup (this, "hide-end").AddDelegate (value); }
            remove { GLib.Signal.Lookup (this, "hide-end").RemoveDelegate (value); }
        }
        
        [GLib.Signal ("request-show")]
        public event EventHandler RequestShowEvent {
            add { GLib.Signal.Lookup (this, "request-show").AddDelegate (value); }
            remove { GLib.Signal.Lookup (this, "request-show").RemoveDelegate (value); }
        }
        
        [GLib.Signal ("request-hide")]
        public event EventHandler RequestHideEvent {
            add { GLib.Signal.Lookup (this, "request-hide").AddDelegate (value); }
            remove { GLib.Signal.Lookup (this, "request-hide").RemoveDelegate (value); }
        }
        
        [GLib.Signal ("request-focus")]
        public event EventHandler RequestFocusEvent {
            add { GLib.Signal.Lookup (this, "request-focus").AddDelegate (value); }
            remove { GLib.Signal.Lookup (this, "request-focus").RemoveDelegate (value); }
        }
        
        // FIXME: need to define an delegate and args class
        
        [GLib.Signal ("request-button-style")]
        public event EventHandler RequestButtonStyleEvent {
            add { GLib.Signal.Lookup (this, "request-button-style").AddDelegate (value); }
            remove { GLib.Signal.Lookup (this, "request-button-style").RemoveDelegate (value); }
        }
        
        [GLib.Signal ("request-tooltip")]
        public event EventHandler RequestTooltipEvent {
            add { GLib.Signal.Lookup (this, "request-tooltip").AddDelegate (value); }
            remove { GLib.Signal.Lookup (this, "request-tooltip").RemoveDelegate (value); }
        }
    }
}
