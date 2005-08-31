/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  NotificationAreaIcon.cs
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
using System.Runtime.InteropServices;
using Gtk;
using Gdk;
using Mono.Posix;

namespace Banshee
{
	public class NotificationAreaIcon
	{
		private Tooltips tooltips;
		private EventBox traybox;

		private TrayIcon ticon;

		private Menu traymenu;
		
		public ImageMenuItem PlayItem;
		public ImageMenuItem NextItem;
		public ImageMenuItem PreviousItem;
		public ImageMenuItem ExitItem;
		public ImageMenuItem RepeatItem;
		public ImageMenuItem ShuffleItem;
		
		private int menu_x;
		private int menu_y;

		public event EventHandler ClickEvent;

		public NotificationAreaIcon()
		{
			ticon = new TrayIcon(Catalog.GetString("Banshee"));
			tooltips = new Tooltips();
			CreateMenu();
			Init();
		} 

		public void Init()
		{
			ticon.DestroyEvent += OnDestroyEvent;

			traybox = new EventBox();
			traybox.ButtonPressEvent += 
				new ButtonPressEventHandler(OnTrayIconClick); 
			traybox.Add(new Gtk.Image(Gdk.Pixbuf.
				LoadFromResource("tray-icon.png")));
			
			ticon.Add(traybox);
			ticon.ShowAll();
		}

		private void OnTrayIconClick(object o, ButtonPressEventArgs args)
		{
			switch(args.Event.Button) {
				case 1:
					if(ClickEvent != null)
						ClickEvent(o, args);
					break;
				case 3:
					menu_x = (int)args.Event.XRoot - (int)args.Event.X;
					menu_y = (int)args.Event.YRoot - (int)args.Event.Y;
					traymenu.Popup(null, null, 
						new MenuPositionFunc(PositionMenu), 
						IntPtr.Zero, args.Event.Button, args.Event.Time);
					break;
			}

			args.RetVal = false;
		}

		private void CreateMenu()
		{
			traymenu = new Menu();
			
			PlayItem = new ImageMenuItem(Catalog.GetString("Play / Pause"));
			PlayItem.Image = new Gtk.Image();
			((Gtk.Image)PlayItem.Image).SetFromStock("media-play", IconSize.Menu);
			traymenu.Append(PlayItem);

			NextItem = new ImageMenuItem(Catalog.GetString("Next"));
			NextItem.Image = new Gtk.Image();
			((Gtk.Image)NextItem.Image).SetFromStock("media-next", IconSize.Menu);
			traymenu.Append(NextItem);

			PreviousItem = new ImageMenuItem(Catalog.GetString("Previous"));
			PreviousItem.Image = new Gtk.Image();
			((Gtk.Image)PreviousItem.Image).SetFromStock("media-prev", 
				IconSize.Menu);
			traymenu.Append(PreviousItem);
			
			traymenu.Append(new SeparatorMenuItem());
			
			ShuffleItem = new ImageMenuItem(Catalog.GetString("Shuffle"));
			ShuffleItem.Image = new Gtk.Image();
			((Gtk.Image)ShuffleItem.Image).SetFromStock("gtk-no", IconSize.Menu);
			traymenu.Append(ShuffleItem);
			
			RepeatItem = new ImageMenuItem(Catalog.GetString("Repeat"));
			RepeatItem.Image = new Gtk.Image();
			((Gtk.Image)RepeatItem.Image).SetFromStock("gtk-no", IconSize.Menu);
			traymenu.Append(RepeatItem);
			
			traymenu.Append(new SeparatorMenuItem());
			
			ExitItem = new ImageMenuItem(Catalog.GetString("Quit Banshee"));
			ExitItem.Image = new Gtk.Image();
			((Gtk.Image)ExitItem.Image).SetFromStock("gtk-quit", IconSize.Menu);
			traymenu.Append(ExitItem);

			traymenu.ShowAll();
		}
		
		private void PositionMenu(Menu menu, out int x, out int y, 
			out bool push_in)
		{
			x = menu_x;
			y = menu_y + 22;
			push_in = true;
		}
		
		private void OnDestroyEvent (object o, DestroyEventArgs args)
		{
			Init ();
		}
		
		public string Tooltip
		{
			set { 
				tooltips.SetTip(traybox, value, null); 
			}
		}	

	}
	
	public class TrayIcon : Plug
	{
		private int stamp;
		private Orientation orientation;
		
		private int selection_atom;
		private int manager_atom;
		private int system_tray_opcode_atom;
		private int orientation_atom;
		private IntPtr manager_window;
		private FilterFunc filter;
		
		public TrayIcon(string name)
		{
			Title = name;
			stamp = 1;
			orientation = Orientation.Horizontal;
			AddEvents ((int)EventMask.PropertyChangeMask);
			filter = new FilterFunc(ManagerFilter);
		}
 
		protected override void OnRealized()
		{
			base.OnRealized();
			Display display = Screen.Display;
			IntPtr xdisplay = gdk_x11_display_get_xdisplay(display.Handle);
			selection_atom = XInternAtom(xdisplay, "_NET_SYSTEM_TRAY_S" 
				+ Screen.Number.ToString(), false);
			manager_atom = XInternAtom(xdisplay, "MANAGER", false);
			system_tray_opcode_atom = XInternAtom(xdisplay, 
				"_NET_SYSTEM_TRAY_OPCODE", false);
			orientation_atom = XInternAtom(xdisplay, 
				"_NET_SYSTEM_TRAY_ORIENTATION", false);
			UpdateManagerWindow();
			//Screen.RootWindow.AddFilter(filter);
		}
 
		protected override void OnUnrealized()
		{
			//if(manager_window != IntPtr.Zero) {
			//	Gdk.Window gdkwin = Gdk.Window.LookupForDisplay(Display, 
			//		(uint)manager_window);
			//	gdkwin.RemoveFilter(filter);
			//}
			
			//Screen.RootWindow.RemoveFilter (filter);
			base.OnUnrealized();
		}
 
		private void UpdateManagerWindow()
		{
			IntPtr xdisplay = gdk_x11_display_get_xdisplay(Display.Handle);
			//if(manager_window != IntPtr.Zero) {
			//	Gdk.Window gdkwin = Gdk.Window.LookupForDisplay(Display, 
			//		(uint)manager_window);
			//	gdkwin.RemoveFilter(filter);
			//}
 
			XGrabServer(xdisplay);
 
			manager_window = XGetSelectionOwner(xdisplay, selection_atom);
			if(manager_window != IntPtr.Zero) {
				XSelectInput(xdisplay, manager_window, 
					EventMask.StructureNotifyMask | 
					EventMask.PropertyChangeMask);
			}
			
			XUngrabServer(xdisplay);
			XFlush(xdisplay);
 
			if(manager_window != IntPtr.Zero) {
				//Gdk.Window gdkwin = Gdk.Window.LookupForDisplay(Display, 
				//	(uint)manager_window);
				//gdkwin.AddFilter(filter);
				
				SendDockRequest();
				GetOrientationProperty();
			}
		}
 
		private void SendDockRequest()
		{
			SendManagerMessage(SystemTrayMessage.RequestDock, 
				manager_window, Id, 0, 0);
		}
 
		private void SendManagerMessage(SystemTrayMessage message, 
			IntPtr window, uint data1, uint data2, uint data3)
		{
			XClientMessageEvent ev = new XClientMessageEvent();
			IntPtr display;
 
			ev.type = XEventName.ClientMessage;
			ev.window = window;
			ev.message_type = (IntPtr)system_tray_opcode_atom;
			ev.format = 32;
			ev.ptr1 = gdk_x11_get_server_time(GdkWindow.Handle);
			ev.ptr2 = (IntPtr)message;
			ev.ptr3 = (IntPtr)data1;
			ev.ptr4 = (IntPtr)data2;
			ev.ptr5 = (IntPtr)data3;
 
			display = gdk_x11_display_get_xdisplay(Display.Handle);
			gdk_error_trap_push();
			XSendEvent(display, manager_window, false, 
				EventMask.NoEventMask, ref ev);
			gdk_error_trap_pop();
		}
 
		private FilterReturn ManagerFilter(IntPtr xevent, Event evnt)
		{
			//TODO: Implement;
			return FilterReturn.Continue;
		}
 
		private void GetOrientationProperty()
		{
			//TODO: Implement;
		}
 
		[DllImport("gdk-x11-2.0")]
		static extern IntPtr gdk_x11_display_get_xdisplay(IntPtr display);
		
		[DllImport("gdk-x11-2.0")]
		static extern IntPtr gdk_x11_get_server_time(IntPtr window);
		
		[DllImport("gdk-x11-2.0")]
		static extern void gdk_error_trap_push();
		
		[DllImport("gdk-x11-2.0")]
		static extern void gdk_error_trap_pop();
		
		
		[DllImport("libX11", EntryPoint="XInternAtom")]
		extern static int XInternAtom(IntPtr display, string atom_name,
			 bool only_if_exists);
		
		[DllImport("libX11")]
		extern static void XGrabServer(IntPtr display);
		
		[DllImport("libX11")]
		extern static void XUngrabServer(IntPtr display);
		
		[DllImport("libX11")]
		extern static int XFlush(IntPtr display);
		
		[DllImport("libX11")]
		extern static IntPtr XGetSelectionOwner(IntPtr display, int atom);
		
		[DllImport("libX11")]
		extern static IntPtr XSelectInput(IntPtr window, IntPtr display, 
			EventMask mask);
		
		[DllImport("libX11", EntryPoint="XSendEvent")]
		extern static int XSendEvent(IntPtr display, IntPtr window, 
			bool propagate, EventMask event_mask, 
			ref XClientMessageEvent send_event);
	}
 
	[Flags]
	internal enum EventMask {
		NoEventMask              = 0,
		KeyPressMask             = 1 << 0,
		KeyReleaseMask           = 1 << 1,
		ButtonPressMask          = 1 << 2,
		ButtonReleaseMask        = 1 << 3,
		EnterWindowMask          = 1 << 4,
		LeaveWindowMask          = 1 << 5,
		PointerMotionMask        = 1 << 6,
		PointerMotionHintMask    = 1 << 7,
		Button1MotionMask        = 1 << 8,
		Button2MotionMask        = 1 << 9,
		Button3MotionMask        = 1 << 10,
		Button4MotionMask        = 1 << 11,
		Button5MotionMask        = 1 << 12,
		ButtonMotionMask         = 1 << 13,
		KeymapStateMask          = 1 << 14,
		ExposureMask             = 1 << 15,
		VisibilityChangeMask     = 1 << 16,
		StructureNotifyMask      = 1 << 17,
		ResizeRedirectMask       = 1 << 18,
        SubstructureNotifyMask   = 1 << 19,
		SubstructureRedirectMask = 1 << 20,
		FocusChangeMask          = 1 << 21,
		PropertyChangeMask       = 1 << 22,
		ColormapChangeMask       = 1 << 23,
		OwnerGrabButtonMask      = 1 << 24
	};
 
	internal enum SystemTrayMessage {
		RequestDock,
		BeginMessage,
		CancelMessage
	};
 
	internal enum SystemTrayOrientation {
		Horz,
		Vert
	};
	
	internal enum XEventName {
		KeyPress                = 2,
		KeyRelease              = 3,
		ButtonPress             = 4,
		ButtonRelease           = 5,
		MotionNotify            = 6,
		EnterNotify             = 7,
		LeaveNotify             = 8,
		FocusIn                 = 9,
		FocusOut                = 10,
		KeymapNotify            = 11,
		Expose                  = 12,
		GraphicsExpose          = 13,
		NoExpose                = 14,
		VisibilityNotify        = 15,
		CreateNotify            = 16,
		DestroyNotify           = 17,
		UnmapNotify             = 18,
		MapNotify               = 19,
		MapRequest              = 20,
		ReparentNotify          = 21,
		ConfigureNotify         = 22,
		ConfigureRequest        = 23,
		GravityNotify           = 24,
		ResizeRequest           = 25,
		CirculateNotify         = 26,
		CirculateRequest        = 27,
		PropertyNotify          = 28,
		SelectionClear          = 29,
		SelectionRequest        = 30,
		SelectionNotify         = 31,
		ColormapNotify          = 32,
		ClientMessage           = 33,
		MappingNotify           = 34,
		TimerNotify             = 100,		
		LASTEvent
	}
	
	[StructLayout(LayoutKind.Sequential)]
	internal struct XClientMessageEvent
	{
		internal XEventName     type;
		internal int            serial;
		internal bool           send_event;
		internal IntPtr         display;
		internal IntPtr         window;
		internal IntPtr         message_type;
		internal int            format;
		internal IntPtr         ptr1;
		internal IntPtr         ptr2;
		internal IntPtr         ptr3;
		internal IntPtr         ptr4;
		internal IntPtr         ptr5;
	}
}
