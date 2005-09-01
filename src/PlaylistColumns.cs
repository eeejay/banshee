/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  PlaylistView.cs
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
