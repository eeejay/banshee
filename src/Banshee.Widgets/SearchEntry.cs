/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  SearchEntry.cs
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
using Mono.Unix;
using Gtk;

namespace Banshee.Widgets
{
    public class SearchEntry : Frame
    {
        private ArrayList searchFields;
        private Hashtable menuMap;
        private Menu popupMenu;
        
        private HBox box;
        private Entry entry;
        private EventBox evBox;
        private EventBox evCancelBox;
        private Image img;
        private Image cancelImage;
        private Tooltips tooltips;
    
        private Gdk.Pixbuf icon;
        private Gdk.Pixbuf hoverIcon;
        
        private Gdk.Cursor handCursor;
        
        private CheckMenuItem activeItem;
        private bool menuActive;
        
        private bool emptyEmitted;
        private uint timeoutId;
        
        public event EventHandler EnterPress;
        public event EventHandler Changed;
    
        public SearchEntry(ArrayList searchFields) : base()
        {
            handCursor = new Gdk.Cursor(Gdk.CursorType.Hand1);
            tooltips = new Tooltips();
            tooltips.Enable();
            
            this.searchFields = searchFields;
            
            BuildWidget();
            BuildMenu();
        }
        
        private void BuildWidget()
        {
            ShadowType = ShadowType.In;
        
            EventBox evContainer = new EventBox();
            Add(evContainer);
            
            box = new HBox();
            evContainer.Add(box);
            
            entry = new Entry();
            entry.HasFrame = false;
            entry.WidthChars = 15;
            entry.Activated += OnEntryActivated;
            entry.KeyPressEvent += OnEntryKeyPressEvent;
            
            IconSet icon_set = IconFactory.LookupDefault(Stock.Find);
            icon = icon_set.RenderIcon(entry.Style, entry.Direction, entry.State, IconSize.Menu, null, null);

            /*Gdk.Pixbuf arrow_overlay = new Gdk.Pixbuf(new string [] {
                "5 3 2 1",
                "   c None",
                ".  c #000000",
                ".....",
                " ... ",
                "  .  "
            });
            
            // Currently this SIGSEGVs in Gdk.Pixbuf:gdk_pixbuf_copy_area
            arrow_overlay.CopyArea(0, 0, arrow_overlay.Width, arrow_overlay.Height, icon, 0, 0);*/
            
            hoverIcon = PixbufColorshift(icon, 30);
            
            img = new Image(icon);
            img.Xpad = 1;
            img.Ypad = 1;
            
            evBox = new EventBox();
            evBox.CanFocus = true;
            evBox.EnterNotifyEvent += OnEnterNotifyEvent;
            evBox.LeaveNotifyEvent += OnLeaveNotifyEvent;
            evBox.ButtonPressEvent += OnButtonPressEvent;
            evBox.KeyPressEvent += OnKeyPressEvent;
            evBox.FocusInEvent += OnFocusInEvent;
            evBox.FocusOutEvent += OnFocusOutEvent;
            evBox.Add(img);
            evBox.ModifyBg(StateType.Normal, entry.Style.Base(StateType.Normal));
            evContainer.ModifyBg(StateType.Normal, entry.Style.Base(StateType.Normal));
            
            cancelImage = new Image(Stock.Clear, IconSize.Menu);
            cancelImage.Xpad = 2;
            evCancelBox = new EventBox();
            evCancelBox.CanFocus = true;
            evCancelBox.Add(cancelImage);
            evCancelBox.ModifyBg(StateType.Normal, entry.Style.Base(StateType.Normal));
            evCancelBox.EnterNotifyEvent += OnCancelEnterNotifyEvent;
            evCancelBox.ButtonPressEvent += OnCancelButtonPressEvent;
            evCancelBox.KeyPressEvent += OnCancelKeyPressEvent;
            
            box.PackStart(evBox, false, false, 0);
            box.PackStart(entry, true, true, 0);
            box.PackStart(evCancelBox, false, false, 0);
            
            evContainer.ShowAll();
            evCancelBox.HideAll();
            
            SetSizeRequest(175, -1);
            
            entry.Changed += OnEntryChanged;
        }
        
        private static byte PixelClamp(int val)
        {
            return (byte)Math.Max(0, Math.Min(255, val));
        }
        
        private unsafe Gdk.Pixbuf PixbufColorshift(Gdk.Pixbuf src, byte shift)
        {
            Gdk.Pixbuf dest = new Gdk.Pixbuf(src.Colorspace, src.HasAlpha, src.BitsPerSample, src.Width, src.Height);
            
            byte *src_pixels_orig = (byte *)src.Pixels;
            byte *dest_pixels_orig = (byte *)dest.Pixels;
            
            for(int i = 0; i < src.Height; i++) {
                byte *src_pixels = src_pixels_orig + i * src.Rowstride;
                byte *dest_pixels = dest_pixels_orig + i * dest.Rowstride;
                
                for(int j = 0; j < src.Width; j++) {
                    *(dest_pixels++) = PixelClamp(*(src_pixels++) + shift);
                    *(dest_pixels++) = PixelClamp(*(src_pixels++) + shift);
                    *(dest_pixels++) = PixelClamp(*(src_pixels++) + shift);
                    
                    if(src.HasAlpha) {
                        *(dest_pixels++) = *(src_pixels++);
                    }
                }
            }
            
            return dest;
        }
        
        private void BuildMenu()
        {
            popupMenu = new Menu();
            menuMap = new Hashtable();
            
            popupMenu.Deactivated += OnMenuDeactivated;
            
            foreach(string menuLabel in searchFields) {
                if(menuLabel.Equals("-")) {
                    popupMenu.Append(new SeparatorMenuItem());
                } else {
                    CheckMenuItem item = new CheckMenuItem(menuLabel);
                    item.DrawAsRadio = true;
                    item.Toggled += OnMenuItemToggled;
                    popupMenu.Append(item);
                    
                    if(activeItem == null) {
                        activeItem = item;
                        item.Active = true;
                    }
                    
                    menuMap[item] = menuLabel;
                }
            }    
            
            tooltips.SetTip(evBox, String.Format(Catalog.GetString("Searching: {0}"), Field), null);
        }
        
        private void ShowMenu(uint time)
        {
            popupMenu.Popup(null, null, new MenuPositionFunc(MenuPosition), 0, time);
            popupMenu.ShowAll();
            img.Pixbuf = hoverIcon;
            menuActive = true;
        }
        
        private void OnEnterNotifyEvent(object o, EnterNotifyEventArgs args)
        {
            img.GdkWindow.Cursor = handCursor;
            img.Pixbuf = hoverIcon;
        }
        
        private void OnLeaveNotifyEvent(object o, LeaveNotifyEventArgs args)
        {
            if(!evBox.HasFocus && !menuActive) {
                img.Pixbuf = icon;
            }
        }
        
        private void OnButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            ShowMenu(args.Event.Time);
        }
        
        private void OnFocusInEvent(object o, EventArgs args)
        {
            img.Pixbuf = hoverIcon;
        }
        
        private void OnFocusOutEvent(object o, EventArgs args)
        {
            if(!menuActive) {
                img.Pixbuf = icon;
            }
        }
        
        private void OnKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if(args.Event.Key != Gdk.Key.Return && args.Event.Key != Gdk.Key.space) {
                return;
            }
                
            ShowMenu(args.Event.Time);
        }

        private void OnEntryKeyPressEvent(object o, KeyPressEventArgs args) 
        {
            if(args.Event.Key != Gdk.Key.Delete) {
                return;
            }
        }

        private void OnCancelEnterNotifyEvent(object o, EnterNotifyEventArgs args)
        {
            cancelImage.GdkWindow.Cursor = handCursor;
        }
        
        private void OnCancelButtonPressEvent(object o, ButtonPressEventArgs args)
        {
            CancelSearch();
        }
        
        private void OnCancelKeyPressEvent(object o, KeyPressEventArgs args)
        {
            if(args.Event.Key != Gdk.Key.Return && args.Event.Key != Gdk.Key.space) {
                return;
            }
                
            CancelSearch();
        }

        private void MenuPosition(Menu menu, out int x, out int y, out bool push_in)
        {
            int pX, pY;
            
            GdkWindow.GetOrigin(out pX, out pY);
            Requisition req = SizeRequest();
            
            x = pX + Allocation.X;
            y = pY + Allocation.Y + req.Height;    
            push_in = true;
        }
        
        private void OnMenuItemToggled(object o, EventArgs args)
        {
            CheckMenuItem item = o as CheckMenuItem;
            if(activeItem == item) {
                item.Active = true;
            } else {
                activeItem = item;
            }
            
            foreach(MenuItem iterItem in popupMenu.Children) {
                if(!(iterItem is CheckMenuItem)) {
                    continue;
                }
                
                CheckMenuItem checkItem = iterItem as CheckMenuItem;
                    
                if(checkItem != activeItem) {
                    checkItem.Active = false;
                }
            }
            
            activeItem.Active = true;
            
            tooltips.SetTip(evBox, String.Format(Catalog.GetString("Searching: {0}"), Field), null);
            
            entry.HasFocus = true;
            
            EventHandler handler = Changed;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }
        
        private void OnMenuDeactivated(object o, EventArgs args)
        {
            img.Pixbuf = icon;
            menuActive = false;
        }
        
        private void OnEntryActivated(object o, EventArgs args)
        {
            EventHandler handler = EnterPress;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }

        private bool OnTimeout () 
        {
            EventHandler handler = Changed;
            if(handler != null && !emptyEmitted) {
                handler(this, new EventArgs());
            }

            emptyEmitted = entry.Text.Length == 0;
            return false;
        }
        
        private void OnEntryChanged(object o, EventArgs args)
        {
            if(entry.Text.Length == 0) {
                entry.HasFocus = true;
                evCancelBox.HideAll();
            } else {
                emptyEmitted = false;
                evCancelBox.ShowAll();
            }

            if(timeoutId > 0) {
                GLib.Source.Remove(timeoutId);
            }
            
            timeoutId = GLib.Timeout.Add(300, OnTimeout);
        }
        
        public void CancelSearch()
        {
            entry.Text = String.Empty;
        }
        
        public string Query {
            get {
                return entry.Text.Trim();
            }
            
            set {
                entry.Text = value;
            }
        }
        
        public string Field {
            get {
                return menuMap[activeItem] as string;
            }
        }
        
        public void Focus()
        {
            entry.HasFocus = true;
        }

        public new bool HasFocus {
            get {
                return entry.HasFocus;
            }
        }
        
        public bool IsQueryAvailable {
            get {
                return Query != null && Query != String.Empty;
            }
        }
    }
}
