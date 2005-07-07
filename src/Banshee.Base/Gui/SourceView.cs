/***************************************************************************
 *  SourceView.cs
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
using Gtk;
using Gdk;
using Pango;

namespace Sonance
{
	public class CellEdit : Entry, CellEditable
	{
		public string path;
	
		public CellEdit() : base()
		{
		
		}
		
		protected CellEdit(System.IntPtr ptr) : base(ptr)
		{
		
		}
	}
	
	public class EditedEventArgs : EventArgs
	{
		public TreePath path;
		public string text;
	}
	
	public delegate void EditedEventHandler(object o, 
		EditedEventArgs args);

	public class SourceView : TreeView
	{
		private ListStore store;
		private Source selectedSource;
		
		public event EventHandler SourceChanged;

		private int currentTimeout = -1;
		private TreeIter timeoutIter;

		static GLib.GType gtype;
		public static new GLib.GType GType
		{
			get {
				if(gtype == GLib.GType.Invalid)
					gtype = RegisterGType(typeof(SourceView));
				return gtype;
			}
		}
		
		/*
		    I like this method better, packing two cell renderers into 
			a column and using pure data functions to set the pixbufs/text
			but there are some flaws when editing... to get a good edit,
			a custom cell renderer is used with a custom editor. BAH
		
		public SourceView()
		{
			TreeViewColumn col = new TreeViewColumn();
			
			CellRendererPixbuf pixbufRender = new CellRendererPixbuf();
			CellRendererText textRender = new CellRendererText();
			
			col.Title = "Source";
			col.PackStart(pixbufRender, true);
			col.PackStart(textRender, true);
			
			col.SetCellDataFunc(pixbufRender, 
				new TreeCellDataFunc(PixbufCellDataFunc));
			col.SetCellDataFunc(textRender, 
				new TreeCellDataFunc(TextCellDataFunc));
				
			AppendColumn(col);
			
			store = new ListStore(typeof(Source));
			Model = store;
			HeadersVisible = false;
			
			RowActivated += new RowActivatedHandler(OnRowActivated);
			
			Gtk.Drag.DestSet(this,DestDefaults.All,
				sourceViewDestEntries, Gdk.DragAction.Copy);
			
			RefreshList();
		}	
					
		protected void PixbufCellDataFunc(TreeViewColumn tree_column,
			CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			CellRendererPixbuf renderer = (CellRendererPixbuf)cell;
			Source source = (Source)store.GetValue(iter, 0);
			
			renderer.Pixbuf = Pixbuf.LoadFromResource(
				source.Type == Source.SourceType.Library ?
					"source-library-icon.png" :
					"source-playlist-icon.png");
		}
		
		protected void TextCellDataFunc(TreeViewColumn tree_column,
			CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			CellRendererText renderer = (CellRendererText)cell;
			Source source = (Source)store.GetValue(iter, 0);
			
			renderer.Text = source.Name;
			renderer.Weight = source.Equals(selectedSource)
				? (int)Pango.Weight.Bold 
				: (int)Pango.Weight.Normal;
			renderer.Editable = source.Type != Source.SourceType.Library;
		}*/

		public SourceView()
		{
			TreeViewColumn col = new TreeViewColumn();
			SourceRowRenderer renderer = new SourceRowRenderer();
			col.Title = "Source";
			col.PackStart(renderer, true);
			col.SetCellDataFunc(renderer, 
				new TreeCellDataFunc(SourceCellDataFunc));
			AppendColumn(col);
			
			store = new ListStore(typeof(Source));
			Model = store;
			HeadersVisible = false;
			
			CursorChanged += OnCursorChanged;
			
			RefreshList();
		}
					
		protected void SourceCellDataFunc(TreeViewColumn tree_column,
			CellRenderer cell, TreeModel tree_model, TreeIter iter)
		{
			SourceRowRenderer renderer = (SourceRowRenderer)cell;
			renderer.view = this;
			renderer.source = (Source)store.GetValue(iter, 0);
			renderer.Selected = renderer.source.Equals(selectedSource);
			renderer.Editable = renderer.source.Type != SourceType.Library;
		}
		
		public void UpdateRow(TreePath path, string text)
		{
			TreeIter iter;
			
			if(!store.GetIter(out iter,path))
				return;
			
			Source source = store.GetValue(iter, 0) as Source;
			source.Name = text;
			QueueDraw();
		}
		
		public void RefreshList()
		{
			Core.ThreadEnter();
			
			store.Clear();
			store.AppendValues(new LibrarySource());
			string [] names = Playlist.ListAll();
			
			if(names == null)
				return;
				
			foreach(string name in names) {
				PlaylistSource plsrc = new PlaylistSource(name);
				plsrc.Updated += OnSourceUpdated;
				store.AppendValues(plsrc);
			}
			
			Core.ThreadLeave();
		} 
		
		private void OnCursorChanged(object o, EventArgs args)
		{				
			if(currentTimeout < 0)		
				currentTimeout = (int)GLib.Timeout.Add(200, OnCursorChangedTimeout);
		}
		
		private bool OnCursorChangedTimeout()
		{
			TreeIter iter;
			TreeModel model;
			
			currentTimeout = -1;
			
			if(!Selection.GetSelected(out model, out iter))
				return false;
			
			Source newSource = store.GetValue(iter, 0) as Source;
			if(selectedSource == newSource)
				return false;
			
			selectedSource = newSource;
			
			QueueDraw();
			
			EventHandler handler = SourceChanged;
			if(handler != null)
				handler(this, new EventArgs());
			
			return false;
		}
		
		public void SelectLibrary()
		{
			Selection.SelectPath(new TreePath("0"));
			OnCursorChanged(this, new EventArgs());
		}
		
		private void OnSourceUpdated(object o, EventArgs args)
		{
			QueueDraw();
		}
		
		public Source GetSource(TreePath path)
		{
			TreeModel model;
			TreeIter iter;
		
			if(store.GetIter(out iter, path))
				return store.GetValue(iter, 0) as Source;
			
			return null;
		}
		
		public Source SelectedSource
		{
			get {
				return selectedSource;
			}
		}
		
		public Source HighlightedSource
		{
			get {
				TreeModel model;
				TreeIter iter;
				
				if(!Selection.GetSelected(out model, out iter))
					return null;
					
				return store.GetValue(iter, 0) as Source;
			}
		}
	}

	public class SourceRowRenderer : CellRendererText
	{
		public bool Selected = false;
		public Source source;
		public SourceView view;
		
		~SourceRowRenderer()
		{
			Dispose();
		}
		
		public SourceRowRenderer()
		{
			Editable = true;
			//Editable = false;
		}
		
		protected SourceRowRenderer(System.IntPtr ptr) : base(ptr)
		{
		
		}
		
		private StateType RendererStateToWidgetState(CellRendererState flags)
		{
			StateType state = StateType.Normal;
			if((CellRendererState.Selected & flags).Equals(
				CellRendererState.Selected))
				state = StateType.Selected;
			return state;
		}
		
		public override void GetSize(Widget widget, ref Gdk.Rectangle cell_area,
			out int x_offset, out int y_offset, out int width, out int height)
		{		
   			int text_x, text_y, text_w, text_h;
   
   			base.GetSize(widget, ref cell_area, out text_x, out text_y, 
   				out text_w, out text_h);
				
			x_offset = 0;
			y_offset = 0;
			width = text_w;
			height = text_h + 5;
		}
		
		protected override void Render(Gdk.Drawable drawable, 
			Widget widget, Gdk.Rectangle background_area, 
			Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, 
			CellRendererState flags)
		{
			Gdk.Window window = drawable as Gdk.Window;
			StateType state = RendererStateToWidgetState(flags);
			Pixbuf icon = Pixbuf.LoadFromResource(
				source.Type == SourceType.Library ?
					"source-library-icon.png" :
					"source-playlist-icon.png");

			Pango.Layout layout = new Pango.Layout(widget.PangoContext);
			
			FontDescription fd = widget.PangoContext.FontDescription.Copy();
			fd.Weight = Selected ? Pango.Weight.Bold : Pango.Weight.Normal;
			
			layout.FontDescription = fd;
			layout.SetMarkup(source.Name);
			
			int layoutWidth, layoutHeight;
			layout.GetPixelSize(out layoutWidth, out layoutHeight);

			window.DrawPixbuf(widget.Style.TextGC(state), icon, 0, 0, 
				cell_area.X + 0, 
				cell_area.Y + ((cell_area.Height - icon.Height) / 2),
				icon.Width, icon.Height,
				RgbDither.None, 0, 0);
		
			window.DrawLayout(widget.Style.TextGC(state), 
				cell_area.X + icon.Width + 6, 
				cell_area.Y + ((cell_area.Height - layoutHeight) / 2) + 1, 
				layout);
		}
		
		public override CellEditable StartEditing(Gdk.Event evnt , Widget widget, 
			string path, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, 
			CellRendererState flags)
		{
			CellEdit text = new CellEdit();
			text.EditingDone += OnEditDone;
			text.Text = source.Name;
			text.path = path;
			text.Show();
			return text;
		}
		
		private void OnEditDone(object o, EventArgs args)
		{
			CellEdit edit = o as CellEdit;
			if(view == null)
				return;
			view.UpdateRow(new TreePath(edit.path), edit.Text);
		}
	}
}
