/***************************************************************************
 *  VolumeButton.cs
 *
 *  Copyright (C) 2005 Ronald S. Bultje; 2007 Novell, Inc.
 *
 *  Ported to Gtk# by Aaron Bockover <abockover@novell.com>
 *    Based on r106 of libbacon/trunk/src/bacon-volume.c in GNOME SVN
 *
 *  Original Authors (BaconVolumeButton/GTK+):
 *    Ronald S. Bultje <rbultje@ronald.bitfreak.net>
 *    Bastien Nocera  <hadess@hadess.net>
 *  Contributions By (BaconVolumeButton/GTK+):
 *    Jan Arne Petersen <jpetersen@jpetersen.org>
 *    Christian Persch <chpe@gnome.org>
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
using System.Runtime.InteropServices;

using Mono.Unix;

using GLib;
using Gtk;

namespace Bacon
{
    public class VolumeButton : Button
    {
        public delegate void VolumeChangedHandler(int volume);
        
        private const int SCALE_SIZE = 100;
        private const int CLICK_TIMEOUT = 250;
    
        private Tooltips tooltips = new Tooltips();
        
        private Window dock;
        private VolumeScale slider;
        private Image image;
        private Button plus;
        private Button minus;
        
        private IconSize size;
        private uint click_id;
        private int direction;
        private int previous_volume;
        private uint pop_time;
        private bool timeout;
        
        private Gdk.Pixbuf [] pixbufs;
        
        public event VolumeChangedHandler VolumeChanged;
        
        public VolumeButton() : this(0.0, 100.0, 5.0, IconSize.SmallToolbar)
        {
        }
        
        public VolumeButton(double min, double max, double step, IconSize size) : base()
        {
            this.size = size;
            
            BuildButton();
            BuildPopup(min, max, step);
        }
        
        public override void Dispose()
        {
            if(dock != null) {
                dock.Destroy();
                dock = null;
            }
            
            if(click_id != 0) {
                GLib.Source.Remove(click_id);
                click_id = 0;
            }
            
            base.Dispose();
        }
        
        private void BuildButton()
        {
            FocusOnClick = false;
            Relief = ReliefStyle.None;
            
            image = new Image();
            image.Show();
            Add(image);
        }
        
        private void BuildPopup(double min, double max, double step)
        {
            dock = new Window(WindowType.Popup);
            dock.Screen = Screen;
            dock.ButtonPressEvent += OnDockButtonPressEvent;
            dock.KeyPressEvent += OnDockKeyPressEvent;
            dock.KeyReleaseEvent += OnDockKeyReleaseEvent;
            dock.ScrollEvent += OnPlusMinusScollEvent;
            
            Frame frame = new Frame();
            frame.Shadow = ShadowType.Out;
            frame.Show();

            dock.Add(frame);

            VBox box = new VBox(false, 0);
            box.Show();

            frame.Add(box);

            Label label = new Label();
            label.Markup = "<b><big>+</big></b>";
            plus = new Button(label);
            plus.Relief = ReliefStyle.None;
            plus.ButtonPressEvent += OnPlusMinusButtonPressEvent;
            plus.ButtonReleaseEvent += OnPlusMinusButtonReleaseEvent;
            plus.ScrollEvent += OnPlusMinusScollEvent;
            plus.ShowAll();
            box.PackStart(plus, false, true, 0);

            slider = new VolumeScale(this, min, max, step);
            slider.SetSizeRequest(-1, SCALE_SIZE);
            slider.DrawValue = false;
            slider.Inverted = true;
            slider.Show();
            box.PackStart(slider, true, true, 0);

            label = new Label();
            label.Markup = "<b><big>\u2212</big></b>";
            minus = new Button(label);
            minus.Relief = ReliefStyle.None;
            minus.ButtonPressEvent += OnPlusMinusButtonPressEvent;
            minus.ButtonReleaseEvent += OnPlusMinusButtonReleaseEvent;
            minus.ScrollEvent += OnPlusMinusScollEvent;
            minus.ShowAll();
            box.PackEnd(minus, false, true, 0);
            
            Show();
        }
        
        protected virtual void OnVolumeChanged()
        {
            VolumeChangedHandler handler = VolumeChanged;
            if(handler != null) {
                handler(Volume);
            }
        }
        
        private bool ShowDock(Gdk.Event evnt)
        {
            Adjustment adj = slider.Adjustment;
            int x, y, m, dx, dy, sx, sy, ystartoff;
            uint event_time;
            double v;
            
            if(previous_volume != (int)slider.Adjustment.Lower) {
                previous_volume = Volume;
            }
            
            if(evnt is Gdk.EventKey) {
                event_time = ((Gdk.EventKey)evnt).Time;
            } else if(evnt is Gdk.EventButton) {
                event_time = ((Gdk.EventButton)evnt).Time;
            } else if(evnt is Gdk.EventScroll) {
                event_time = ((Gdk.EventScroll)evnt).Time;
            } else {
                throw new ApplicationException("ShowDock expects EventKey, EventButton, or EventScroll");
            }
        
            dock.Screen = Screen;
            
            GdkWindow.GetOrigin(out x, out y);
            x += Allocation.X;
            y += Allocation.Y;
            
            dock.Move(x, y - (SCALE_SIZE / 2));
            dock.ShowAll();
            
            dock.GdkWindow.GetOrigin(out dx, out dy);
            dy += dock.Allocation.Y;
            
            slider.GdkWindow.GetOrigin(out sx, out sy);
            sy += slider.Allocation.Y;
            ystartoff = sy - dy;
            
            timeout = true;
            
            v = Volume / (adj.Upper - adj.Lower);
            x += (Allocation.Width - dock.Allocation.Width) / 2;
            y -= ystartoff;
            y -= slider.MinSliderSize / 2;
            m = slider.Allocation.Height - slider.MinSliderSize;
            y -= (int)(m * (1.0 - v));
            
            if(evnt is Gdk.EventButton) {
                y += (int)((Gdk.EventButton)evnt).Y;
            } else if(evnt is Gdk.EventScroll) {
                y += (int)((Gdk.EventScroll)evnt).Y;
            }
            
            dock.Move(x, y);
            slider.GdkWindow.GetOrigin(out sx, out sy);
            
            bool base_result = evnt is Gdk.EventButton 
                ? base.OnButtonPressEvent((Gdk.EventButton)evnt) 
                : true;
            
            Gtk.Grab.Add(dock);
            
            if(Gdk.Pointer.Grab(dock.GdkWindow, true, 
                Gdk.EventMask.ButtonPressMask | 
                Gdk.EventMask.ButtonReleaseMask | 
                Gdk.EventMask.PointerMotionMask, null, null, event_time) != Gdk.GrabStatus.Success) {
                Gtk.Grab.Remove(dock);
                dock.Hide();
                return false;
            }
            
            if(Gdk.Keyboard.Grab(dock.GdkWindow, true, event_time) != Gdk.GrabStatus.Success) {
                Display.PointerUngrab(event_time);
                Gtk.Grab.Remove(dock);
                dock.Hide();
                return false;
            }
            
            if(evnt is Gdk.EventButton) {
                dock.GrabFocus();
            
                Gdk.EventButton evnt_copy = (Gdk.EventButton)Gdk.EventHelper.Copy(evnt);
                m = slider.Allocation.Height - slider.MinSliderSize;
                UpdateEventButton(evnt_copy, slider.GdkWindow, slider.Allocation.Width / 2, 
                    ((1.0 - v) * m) + slider.MinSliderSize / 2);
                slider.ProcessEvent(evnt_copy);
                Gdk.EventHelper.Free(evnt_copy);
            } else {
                slider.GrabFocus();
            }
                   
            pop_time = event_time;
            
            return base_result;
        }
        
        protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
        {
            return ShowDock(evnt);
        }
        
        protected override bool OnKeyReleaseEvent(Gdk.EventKey evnt)
        {
            switch(evnt.Key) {
                case Gdk.Key.space:
                case Gdk.Key.Return:
                case Gdk.Key.KP_Enter:
                    return ShowDock(evnt);
                default:
                    return false;
            }
        }
        
        protected override bool OnKeyPressEvent(Gdk.EventKey evnt)
        {
            switch(evnt.Key) {
                case Gdk.Key.Up:
                case Gdk.Key.Down:
                case Gdk.Key.Left:
                case Gdk.Key.Right:
                case Gdk.Key.KP_Add:
                case Gdk.Key.KP_Subtract:
                case Gdk.Key.plus:
                case Gdk.Key.minus:
                    return ShowDock(evnt);
                case Gdk.Key.Key_0:
                case Gdk.Key.KP_0:
                case Gdk.Key.m:
                case Gdk.Key.M:
                    ToggleMute();
                    return false;
                default:
                    break;
            }
            
            return false;
        }
        
        protected override bool OnScrollEvent(Gdk.EventScroll evnt)
        {
            if(evnt.Type != Gdk.EventType.Scroll) {
                return false;
            }
            
            /*if(evnt.Direction == Gdk.ScrollDirection.Up) {
                AdjustVolume(1);
            } else if(evnt.Direction == Gdk.ScrollDirection.Down) {
                AdjustVolume(-1);
            }*/
            
            ShowDock(evnt);
            
            return true;
        }
        
        protected override void OnStyleSet(Style previous)
        {
            base.OnStyleSet(previous);
            LoadIcons();
        }
        
        private void OnDockKeyPressEvent(object o, KeyPressEventArgs args)
        {
            args.RetVal = args.Event.Key == Gdk.Key.Escape;
        }
        
        private void OnDockKeyReleaseEvent(object o, KeyReleaseEventArgs args)
        {
            if(args.Event.Key == Gdk.Key.Escape) {
                Display.KeyboardUngrab(args.Event.Time);
                Display.PointerUngrab(args.Event.Time);
                Gtk.Grab.Remove(dock);
                dock.Hide();
                timeout = false;
                args.RetVal = true;
                return;
            }
            
            args.RetVal = false;
        }
        
        private void OnDockButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if(args.Event.Type == Gdk.EventType.ButtonPress) {
                ReleaseGrab(args.Event);
                args.RetVal = true;
                return;
            }
            
            args.RetVal = false;
        }
        
        private bool PlusMinusButtonTimeout()
        {
            if(click_id == 0) {
                return false;
            }
            
            bool result = AdjustVolume(direction);
            
            if(!result) {
                GLib.Source.Remove(click_id);
                click_id = 0;
            }
            
            return result;
        }
        
        [GLib.ConnectBefore]
        private void OnPlusMinusButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            if(click_id != 0) {
                GLib.Source.Remove(click_id);
            }
            
            direction = o == minus ? -1 : 1;
            
            click_id = GLib.Timeout.Add(CLICK_TIMEOUT / 2, PlusMinusButtonTimeout);
            PlusMinusButtonTimeout();
            
            args.RetVal = true;
        }
        
        [GLib.ConnectBefore]
        private void OnPlusMinusButtonReleaseEvent(object o, ButtonReleaseEventArgs args)
        {
            if(click_id != 0) {
                GLib.Source.Remove(click_id);
                click_id = 0;
            }
        }
        
        [GLib.ConnectBefore]
        private void OnPlusMinusScollEvent(object o, ScrollEventArgs args)
        {
            if(args.Event.Direction == Gdk.ScrollDirection.Up) {
                AdjustVolume(1);
            } else if(args.Event.Direction == Gdk.ScrollDirection.Down) {
                AdjustVolume(-1);
            }
        }
        
        private void ReleaseGrab(Gdk.Event evnt)
        {       
            uint event_time;
                 
            if(evnt is Gdk.EventKey) {
                event_time = ((Gdk.EventKey)evnt).Time;
            } else if(evnt is Gdk.EventButton) {
                event_time = ((Gdk.EventButton)evnt).Time;
            } else {
                throw new ApplicationException("ShowDock expects EventKey or EventButton");
            }
            
            Display.KeyboardUngrab(event_time);
            Display.PointerUngrab(event_time);
            Gtk.Grab.Remove(dock);
            
            dock.Hide();
            timeout = false;
            
            if(evnt is Gdk.EventButton) {
                Gdk.EventButton evnt_copy = (Gdk.EventButton)Gdk.EventHelper.Copy(evnt);
                UpdateEventButton(evnt_copy, GdkWindow, Gdk.EventType.ButtonRelease);
                ProcessEvent(evnt_copy);
                Gdk.EventHelper.Free(evnt_copy);
            }
        }
        
        private void LoadIcons()
        {
            string [,] icon_names = { 
                { "audio-volume-muted",  "stock_volume-0"   },
                { "audio-volume-low",    "stock_volume-min" },
                { "audio-volume-medium", "stock_volume-med" },
                { "audio-volume-high",   "stock_volume-max" }
            };

            int width, height;
            Icon.SizeLookup(size, out width, out height);
            IconTheme theme = IconTheme.GetForScreen(Screen);
            
            if(pixbufs == null) {
                pixbufs = new Gdk.Pixbuf[icon_names.Length / icon_names.Rank];
            }
            
            for(int i = 0; i < icon_names.Length / icon_names.Rank; i++) {
                for(int j = 0; j < icon_names.Rank; j++) {
                    if(pixbufs[i] != null) {
                        pixbufs[i].Dispose();
                        pixbufs[i] = null;
                    }
                    
                    try {
                        pixbufs[i] = theme.LoadIcon(icon_names[i, j], width, 0);
                        break;
                    } catch {
                    }
                }
            }
            
            Update();
        }
        
        private void Update()
        {
            UpdateIcon();
            UpdateTip();
        }
        
        private void UpdateIcon()
        {
            if(slider == null || pixbufs == null) {
                return;
            }
            
            double step = (slider.Adjustment.Upper - slider.Adjustment.Lower - 1) / (pixbufs.Length - 1);
            image.Pixbuf = pixbufs[(int)Math.Ceiling((Volume - 1) / step)];
        }
        
        private void UpdateTip()
        {
            string tip;
            
            if(Volume == slider.Adjustment.Lower) {
                tip = Catalog.GetString("Muted");
            } else if(Volume == slider.Adjustment.Upper) {
                tip = Catalog.GetString("Full Volume");
            } else {
                tip = String.Format("{0}%", (int)((Volume - slider.Adjustment.Lower) /
                    (slider.Adjustment.Upper - slider.Adjustment.Lower) * 100.0));
            }
            
            tooltips.SetTip(this, tip, null);
        }
        
        private bool AdjustVolume(int direction) 
        {
            Adjustment adj = slider.Adjustment;
            
            double temp_vol = Volume + direction * adj.StepIncrement;
            temp_vol = Math.Min(adj.Upper, temp_vol);
            temp_vol = Math.Max(adj.Lower, temp_vol);

            Volume = (int)temp_vol;

            return Volume > adj.Lower && Volume < adj.Upper;
        }
        
        public void ToggleMute()
        {
            if(Volume == (int)slider.Adjustment.Lower) {
                Volume = previous_volume;
            } else {
                previous_volume = Volume;
                Volume = (int)slider.Adjustment.Lower;
            }
        }
        
        public int Volume {
            get { return (int)slider.Value; }
            set { 
                slider.Value = value; 
                Update();
            }
        }
        
        // FIXME: This is seriously LAME. The Gtk# binding does not support mutating
        // Gdk.Event* objects. All the properties are marked read only. Support for
        // these objects is simply incomplete.
        // http://bugzilla.ximian.com/show_bug.cgi?id=80685
        
        private void UpdateEventButton(Gdk.EventButton evnt, Gdk.Window window, Gdk.EventType type)
        {
            Marshal.WriteInt32(evnt.Handle, 0, (int)type);
            UpdateEventButtonWindow(evnt, window);
        }
        
        private void UpdateEventButton(Gdk.EventButton evnt, Gdk.Window window, double x, double y)
        {
            int x_offset = IntPtr.Size * 2 + 8;
       
            UpdateEventButtonWindow(evnt, window);                    
            MarshalWriteDouble(evnt.Handle, x_offset, x);
            MarshalWriteDouble(evnt.Handle, x_offset + 8, y);
        }
    
        private void UpdateEventButtonWindow(Gdk.EventButton evnt, Gdk.Window window)
        {
            // FIXME: GLib.Object.Ref is obsolete because it's low level and shouldn't
            // be exposed, but it was in 1.x, and it's not going to go away, so this is OK.
            
            #pragma warning disable 0612
            window.Ref();
            #pragma warning restore 0612
            
            Marshal.WriteIntPtr(evnt.Handle, IntPtr.Size, window.Handle);
        }
    
        private void MarshalWriteDouble(IntPtr ptr, int offset, double value)
        {
            byte [] bytes = BitConverter.GetBytes(value);
            for(int i = 0; i < bytes.Length; i++) {
                Marshal.WriteByte(ptr, offset + i, bytes[i]);
            }
        }

        private class VolumeScale : VScale
        {
            private VolumeButton button;
            
            public VolumeScale(VolumeButton button, double min, double max, double step)
                : base(new Adjustment(min, min, max, step, 10 * step, 0))
            {
                this.button = button;
            }
            
            protected override void OnValueChanged()
            {
                base.OnValueChanged();
                button.Update();
                button.OnVolumeChanged();
            }
            
            protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
            {
                Gtk.Grab.Remove(button.dock);
                return base.OnButtonPressEvent(evnt);
            }
            
            protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
            {
                if(button.timeout) {
                    if(evnt.Time > button.pop_time + CLICK_TIMEOUT) {
                        button.ReleaseGrab(evnt);
                        return base.OnButtonReleaseEvent(evnt);
                    }
                    
                    button.timeout = false;
                }
            
                bool result = base.OnButtonReleaseEvent(evnt);
                
                Gtk.Grab.Add(button.dock);
                
                return result;
            }
            
            protected override bool OnKeyReleaseEvent(Gdk.EventKey evnt)
            {
                switch(evnt.Key) {
                    case Gdk.Key.space:
                    case Gdk.Key.Return:
                    case Gdk.Key.KP_Enter:
                        button.ReleaseGrab(evnt);
                        break;
                }
            
                return base.OnKeyReleaseEvent(evnt);
            }
            
            // FIXME: This is also seriously LAME. The MinSliderSize property is "protected"
            // according to gtkrange.h, and thus should be exposed and accessible through 
            // this sub-class, but GAPI does not bind protected structure fields. LAME LAME.
            // http://bugzilla.ximian.com/show_bug.cgi?id=80684
            
            [DllImport("libgobject-2.0-0.dll")]
            private static extern void g_type_query(IntPtr type, IntPtr query);
            
            // In case there's no map provided by the assembly .config file
            [DllImport("libgobject-2.0.so.0", EntryPoint="g_type_query")]
            private static extern void g_type_query_fallback(IntPtr type, IntPtr query);

            private int min_slider_size_offset = -1;

            public int MinSliderSize {
                get { 
                    if(min_slider_size_offset < 0) {
                        IntPtr query = Marshal.AllocHGlobal(5 * IntPtr.Size);
                        
                        try {
                            g_type_query(Gtk.Widget.GType.Val, query);
                        } catch(DllNotFoundException) {
                            g_type_query_fallback(Gtk.Widget.GType.Val, query);
                        }
                        
                        min_slider_size_offset = (int)Marshal.ReadIntPtr(query, 2 * IntPtr.Size + 4); 
                        min_slider_size_offset += IntPtr.Size + 8;
                            
                        Marshal.FreeHGlobal(query);
                    }

                    return (int)Marshal.ReadIntPtr(Handle, min_slider_size_offset);
                }
            }
        }
    }
}
