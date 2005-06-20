/***************************************************************************
 *  MmKeys.cs
 *
 *  Copyright (C) 2004 Lee Willis <lee@leewillis.co.uk>
 *  Copyright (C) 2004, 2005 Jorn Baayen <jbaayen@gnome.org>
 *  Copyright (C) 2005 Aaron Bockover <aaron@aaronbock.net>
 *  [This file was adapted from Muine]
 ****************************************************************************/

/* MUST SCRAP OR RELICENSE!!! */

/*
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Library General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 */
 
using System;
using System.Collections;
using System.Runtime.InteropServices;

using Gtk;
using GLib;

namespace Sonance
{
	public class MmKeys : GLib.Object
	{
		private SignalUtils.SignalDelegate toggle_play_cb;
		private SignalUtils.SignalDelegate prev_cb;
		private SignalUtils.SignalDelegate next_cb;

		public event EventHandler TogglePlay;
		public event EventHandler Next;
		public event EventHandler Previous;
		
		[DllImport("libsonance")]
		private static extern IntPtr mmkeys_new();

		public MmKeys() : base(IntPtr.Zero)
		{
			base.Raw = mmkeys_new();

			toggle_play_cb = new SignalUtils.SignalDelegate(OnTogglePlay);
			prev_cb = new SignalUtils.SignalDelegate(OnPrevious);
			next_cb  = new SignalUtils.SignalDelegate(OnNext);

			SignalUtils.SignalConnect(base.Raw, "mm_playpause", toggle_play_cb);
			SignalUtils.SignalConnect(base.Raw, "mm_prev", prev_cb);
			SignalUtils.SignalConnect(base.Raw, "mm_next", next_cb);
		}

		~MmKeys()
		{
			Dispose();
		}

		private void OnTogglePlay(IntPtr obj)
		{
			if(TogglePlay != null)
				TogglePlay(this, new EventArgs());
		}

		private void OnNext(IntPtr obj)
		{
			if(Next != null)
				Next(this, new EventArgs());
		}
	
		private void OnPrevious(IntPtr obj)
		{
			if(Previous != null)
				Previous(this, new EventArgs());
		}
	}
}
