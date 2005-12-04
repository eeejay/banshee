/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  VolumeButton.cs
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


using GLib;
using Gtk;
using System;

namespace Banshee.Widgets
{
	public class VolumeButton : ToggleButton
	{
		private Image icon;
		private Window popup;
		private int volume;
		private int revert_volume;

		/* GDK_CURRENT_TIME doesn't seem to have an equiv in gtk-sharp yet. */
		const uint CURRENT_TIME = 0;

		public int Volume {
			set {
				string id = "audio-volume-";
				
				volume = value;

				if (volume <= 0)
					id += "muted";
				else if (volume <= 100 / 3)
					id += "low";
				else if (volume <= 200 / 3)
					id += "medium";
				else
					id += "high";

			
				icon.SetFromStock(id, IconSize.LargeToolbar);
				// don't call VolumeChanged() here as callback in playlist
				// window sets this property so will infinite loop!
				// it's called so we can update various volume UI elements
			}

			get {
				return volume;
			}
		}

		public delegate void VolumeChangedHandler (int vol);
		public event VolumeChangedHandler VolumeChanged;

		public VolumeButton () : base ()
		{
			icon = new Image ();
			icon.Show ();
			Add (icon);
			
			popup = null;

			ScrollEvent += new ScrollEventHandler (ScrollHandler);
			Toggled += new EventHandler (ToggleHandler);
			
			//Flags |= (int) WidgetFlags.NoWindow;
		}

		~VolumeButton ()
		{
			Dispose ();
		}

		private void ShowScale ()
		{
			VScale scale;
			VBox box;
			Adjustment adj;
			Frame frame;
			Image image;
			Requisition req;
			int x, y;

			revert_volume = Volume;

			popup = new Window (WindowType.Popup);
			popup.Screen = this.Screen;

			frame = new Frame ();
			frame.Shadow = ShadowType.Out;
			frame.Show ();

			popup.Add (frame);

			box = new VBox (false, 0);
			box.Show();

			frame.Add (box);

			adj = new Adjustment (volume, 0, 100, 5, 10, 0);		

			scale = new VScale (adj);
			scale.ValueChanged += new EventHandler (ScaleValueChanged);
			scale.KeyPressEvent += new KeyPressEventHandler (ScaleKeyPressed);
			popup.ButtonPressEvent += 
				new ButtonPressEventHandler(PopupButtonPressed);

			scale.Adjustment.Upper = 100.0;
			scale.Adjustment.Lower = 0.0;
			scale.DrawValue = false;
			scale.UpdatePolicy = UpdateType.Continuous;
			scale.Inverted = true;

			scale.Show ();


			image = new Image("audio-volume-increase", IconSize.Menu);
			image.Show ();
			box.PackStart (image, false, true, 0);

			image = new Image("audio-volume-decrease", IconSize.Menu);
			image.Show ();
			box.PackEnd (image, false, true, 0);

			box.PackStart (scale, true, true, 0);

			req = SizeRequest ();

			GdkWindow.GetOrigin (out x, out y);

			scale.SetSizeRequest (-1, 100);
			popup.SetSizeRequest (req.Width, -1);

			popup.Move (x + Allocation.X, y + Allocation.Y + req.Height);
			popup.Show ();

			popup.GrabFocus ();

			Grab.Add (popup);

			Gdk.GrabStatus grabbed = Gdk.Pointer.Grab (popup.GdkWindow, true, 
				Gdk.EventMask.ButtonPressMask | 
				Gdk.EventMask.ButtonReleaseMask | 
				Gdk.EventMask.PointerMotionMask, 
			 null, null, 
			CURRENT_TIME);

			if (grabbed == Gdk.GrabStatus.Success) {
				grabbed = Gdk.Keyboard.Grab (popup.GdkWindow, 
					true, CURRENT_TIME);

				if (grabbed != Gdk.GrabStatus.Success) {
					Grab.Remove (popup);
					popup.Destroy ();
					popup = null;
				}
			} else {
				Grab.Remove (popup);
				popup.Destroy ();
				popup = null;
			}
		}

		private void HideScale ()
		{
			if (popup != null) {
				Grab.Remove (popup);
				Gdk.Pointer.Ungrab (CURRENT_TIME);
				Gdk.Keyboard.Ungrab (CURRENT_TIME);

				popup.Destroy ();
				popup = null;
			}

			Active = false;
		}

		private void ToggleHandler (object obj, EventArgs args)
		{
			if (Active) {
				ShowScale ();
			} else {
				HideScale ();
			}
		}

		private void ScrollHandler (object obj, ScrollEventArgs args)
		{
			int tmp_vol = Volume;
			
			switch (args.Event.Direction) {
			case Gdk.ScrollDirection.Up:
				tmp_vol += 10;
				break;
			case Gdk.ScrollDirection.Down:
				tmp_vol -= 10;
				break;
			default:
				break;
			}

			// A CLAMP equiv doesn't seem to exist ... doing that manually
			tmp_vol = Math.Min (100, tmp_vol);
			tmp_vol = Math.Max (0, tmp_vol);

			Volume = tmp_vol;

			VolumeChanged (Volume);
		}

		private void ScaleValueChanged (object obj, EventArgs args)
		{
			Volume = (int)((VScale)obj).Value;

			VolumeChanged (Volume);
		}

		private void ScaleKeyPressed (object obj, KeyPressEventArgs args)
		{
			switch (args.Event.Key) {
			case Gdk.Key.Escape:
				HideScale ();
				Volume = revert_volume;
				break;
			case Gdk.Key.KP_Enter:
			case Gdk.Key.ISO_Enter:
			case Gdk.Key.Key_3270_Enter:
			case Gdk.Key.Return:
			case Gdk.Key.space:
			case Gdk.Key.KP_Space:
				HideScale ();
				break;
			default:
				break;
			}
		}

		private void PopupButtonPressed (object obj, ButtonPressEventArgs args)
		{
			if (popup != null) {
				HideScale ();
			}
		}
	}
}
