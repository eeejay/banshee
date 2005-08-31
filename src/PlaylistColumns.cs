/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  PlaylistView.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
 ****************************************************************************/

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
using Mono.Unix;
using Gtk;

namespace Banshee
{
	public class PlaylistColumn
	{
		public string Name;
		public TreeViewColumn Column;
		public int Order;
	
		private PlaylistView view;
		private TreeCellDataFunc datafunc;
		private string keyName;
	
		public PlaylistColumn(PlaylistView view, string name, string keyName,
			TreeCellDataFunc datafunc, CellRenderer renderer, 
			int Order, int SortId)
		{
			this.Name = name;
			this.keyName = keyName;
			this.datafunc = datafunc;
			this.Order = Order;
			this.view = view;
			
			Column = new TreeViewColumn();
			Column.Title = name;
			Column.Resizable = true;
			Column.Reorderable = true;
			Column.Sizing = TreeViewColumnSizing.Fixed;
			Column.PackStart(renderer, false);
			Column.SetCellDataFunc(renderer, datafunc);
				
			if(SortId >= 0) {
				Column.Clickable = true;
				Column.SortColumnId = SortId;
			} else {
				Column.Clickable = false;
				Column.SortColumnId = -1;
				Column.SortIndicator = false;
			}
			
			try {
				int width = (int)Core.GconfClient.Get(
					GConfKeys.ColumnPath + keyName + "/Width");
					
				if(width <= 1)
					throw new Exception(Catalog.GetString("Invalid column width"));
					
				Column.FixedWidth = width;
					
				Column.Visible = (bool)Core.GconfClient.Get(
					GConfKeys.ColumnPath + keyName + "/Visible");
				this.Order = (int)Core.GconfClient.Get(
					GConfKeys.ColumnPath + keyName + "/Order");
			} catch(Exception) { 
				Column.FixedWidth = 75;
			}
		}
		
		public void Save(TreeViewColumn [] columns)
		{
			// find current order
			int order_t = 0,  n = columns.Length;
			for(; order_t < n; order_t++)
				if(columns[order_t].Equals(Column))
					break;
					
			Core.GconfClient.Set(GConfKeys.ColumnPath + 
				keyName + "/Width", Column.Width);
			Core.GconfClient.Set(GConfKeys.ColumnPath + 
				keyName + "/Visible", Column.Visible);
			Core.GconfClient.Set(GConfKeys.ColumnPath + 
				keyName + "/Order", order_t);	
		}
	}

	public class PlaylistColumnChooserDialog : Gtk.Window
	{	
		private Hashtable boxes;
	
		static GLib.GType gtype;
		public static new GLib.GType GType
		{
			get {
				if(gtype == GLib.GType.Invalid)
					gtype = RegisterGType(typeof(PlaylistColumnChooserDialog));
				return gtype;
			}
		}

		public PlaylistColumnChooserDialog(ArrayList columns) 
			: base(Catalog.GetString("Choose Columns"))
		{
			BorderWidth = 10;
			SetPosition(WindowPosition.Center);
			TypeHint = Gdk.WindowTypeHint.Utility;
			Resizable = false;
		
			VBox vbox = new VBox();
			vbox.Spacing = 10;
			vbox.Show();
			
			Add(vbox);
			
			Label label = new Label();
			label.Markup = "<b>" + Catalog.GetString("Visible Playlist Columns") + "</b>";
			label.Show();
			vbox.Add(label);
			
			Table table = new Table(
				(uint)System.Math.Ceiling((double)columns.Count), 
				2, false);

			table.Show();
			table.ColumnSpacing = 15;
			table.RowSpacing = 5;
			vbox.Add(table);

			boxes = new Hashtable();
						
			int i = 0;
			foreach(PlaylistColumn plcol in columns) {
				CheckButton cbtn = new CheckButton(plcol.Name);
				boxes[cbtn] = plcol;
				cbtn.Show();
				cbtn.Toggled += OnCheckButtonToggled;
				cbtn.Active = plcol.Column.Visible;
				table.Attach(cbtn, 
					(uint)(i % 2), 
					(uint)((i % 2) + 1), 
					(uint)(i / 2), 
					(uint)(i / 2) + 1,
					AttachOptions.Fill,
					AttachOptions.Fill,
					0, 0);
				i++;
			}
			
			HButtonBox actionArea = new HButtonBox();
			actionArea.Show();
			actionArea.Layout = ButtonBoxStyle.End;	
			
			Button closeButton = new Button("gtk-close");
			closeButton.Clicked += OnCloseButtonClicked;
			closeButton.Show();
			actionArea.PackStart(closeButton);

			vbox.Add(actionArea);
		}
		
		private void OnCheckButtonToggled(object o, EventArgs args)
		{
			CheckButton button = (CheckButton)o;
			
			((PlaylistColumn)boxes[button]).Column.Visible = button.Active;
		}
		
		private void OnCloseButtonClicked(object o, EventArgs args)
		{
			Destroy();
		}
	}
}
