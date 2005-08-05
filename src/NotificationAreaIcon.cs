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
using Mono.Posix;

namespace Banshee
{
	public class NotificationAreaIcon : Plug
	{
		private Tooltips tooltips;
		private EventBox traybox;

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

		public NotificationAreaIcon() : base(IntPtr.Zero)
		{
			tooltips = new Tooltips();
			CreateMenu();
			Init();
		} 

		~NotificationAreaIcon()
		{
			Dispose();
		}

		[DllImport("libsonance")]
		private static extern IntPtr egg_tray_icon_new(string name);

		public void Init()
		{
			Raw = egg_tray_icon_new("Banshee");

			DestroyEvent += new DestroyEventHandler (OnDestroyEvent);

			traybox = new EventBox();
			traybox.ButtonPressEvent += 
				new ButtonPressEventHandler(OnTrayIconClick); 
			traybox.Add(new Image(Gdk.Pixbuf.
				LoadFromResource("tray-icon.png")));
			
			Add(traybox);
			ShowAll();
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
			
			PlayItem = new ImageMenuItem("Play / Pause");
			PlayItem.Image = new Image();
			((Image)PlayItem.Image).SetFromStock("media-play", IconSize.Menu);
			traymenu.Append(PlayItem);

			NextItem = new ImageMenuItem("Next");
			NextItem.Image = new Image();
			((Image)NextItem.Image).SetFromStock("media-next", IconSize.Menu);
			traymenu.Append(NextItem);

			PreviousItem = new ImageMenuItem("Previous");
			PreviousItem.Image = new Image();
			((Image)PreviousItem.Image).SetFromStock("media-prev", 
				IconSize.Menu);
			traymenu.Append(PreviousItem);
			
			traymenu.Append(new SeparatorMenuItem());
			
			ShuffleItem = new ImageMenuItem("Shuffle");
			ShuffleItem.Image = new Image();
			((Image)ShuffleItem.Image).SetFromStock("gtk-no", IconSize.Menu);
			traymenu.Append(ShuffleItem);
			
			RepeatItem = new ImageMenuItem("Repeat");
			RepeatItem.Image = new Image();
			((Image)RepeatItem.Image).SetFromStock("gtk-no", IconSize.Menu);
			traymenu.Append(RepeatItem);
			
			traymenu.Append(new SeparatorMenuItem());
			
			ExitItem = new ImageMenuItem("Quit Banshee");
			ExitItem.Image = new Image();
			((Image)ExitItem.Image).SetFromStock("gtk-quit", IconSize.Menu);
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
}
