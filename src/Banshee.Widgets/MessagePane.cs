// Derived from Beagle's 'Base' Widget: beagle/search/Pages/Base.cs

using Gtk;
using System;
using Mono.Unix;

namespace Banshee.Widgets 
{
	public class MessagePane : Fixed {

		Gtk.Table table;
		Gtk.Image headerIcon;
		Gtk.Label header;
		Gdk.Pixbuf arrow;

		public MessagePane ()
		{
			HasWindow = true;

			table = new Gtk.Table (1, 2, false);
			table.RowSpacing = table.ColumnSpacing = 12;

			headerIcon = new Gtk.Image ();
			headerIcon.Yalign = 0.0f;
			table.Attach (headerIcon, 0, 1, 0, 1,
				      0, Gtk.AttachOptions.Fill,
				      0, 0);

			header = new Gtk.Label ();
			header.SetAlignment (0.0f, 0.5f);
			table.Attach (header, 1, 2, 0, 1,
				      Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill,
				      Gtk.AttachOptions.Fill,
				      0, 0);

			table.ShowAll ();
			Add (table);
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			ModifyBg (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));
		}

		public Gdk.Pixbuf HeaderIcon {
			set {
				headerIcon.Pixbuf = value;
			}
		}
		
		public Gdk.Pixbuf ArrowIcon {
			set {
				arrow = value;
			}
		}

		public string HeaderIconStock {
			set {
				headerIcon.SetFromStock (value, Gtk.IconSize.Dnd);
			}
		}

		public string HeaderMarkup {
			set {
				header.Markup = value;
			}
		}
		
		private void AttachArrow (Gdk.Pixbuf arrow)
		{
			uint row = table.NRows;
			
			Gtk.Image image = arrow == null ? new Gtk.Image (this.arrow) : new Gtk.Image(arrow);
			image.Yalign = 0.0f;
			image.Xalign = 1.0f;
			image.Show ();
			table.Attach (image, 0, 1, row, row + 1,
				      Gtk.AttachOptions.Fill,
				      Gtk.AttachOptions.Fill,
				      0, 0);
		}

		public void Append (string tip)
		{
			Append (tip, true);
		}

		public void Append (string tip, bool showArrow)
		{
		    Append(tip, showArrow, null);  
		}
		
		public void Append (string tip, bool showArrow, Gdk.Pixbuf arrow)
		{
			uint row = table.NRows;
			Gtk.Label label;

			if (showArrow) {
				AttachArrow (arrow);
			}

			label = new Gtk.Label ();
			label.Markup = tip;
			label.SetAlignment (0.0f, 0.5f);
			label.LineWrap = true;
			label.ModifyFg (Gtk.StateType.Normal, label.Style.Foreground (Gtk.StateType.Insensitive));
			label.Show ();
			table.Attach (label, 1, 2, row, row + 1,
				      Gtk.AttachOptions.Expand | Gtk.AttachOptions.Fill,
				      0, 0, 0);
		}

		public void Append (Gtk.Widget widget)
		{
			Append (widget, 0, 0, false);
		}

		public void Append (Gtk.Widget widget, Gtk.AttachOptions xoptions, 
			Gtk.AttachOptions yoptions, bool showArrow)
		{
		    Append(widget, xoptions, yoptions, showArrow, null);
		}

		public void Append (Gtk.Widget widget, Gtk.AttachOptions xoptions, 
			Gtk.AttachOptions yoptions, bool showArrow, Gdk.Pixbuf arrow)
		{
			uint row = table.NRows;

			if (showArrow) {
				AttachArrow(arrow);
			}

			table.Attach (widget, 1, 2, row, row + 1, xoptions, yoptions, 0, 0);
			widget.ModifyBg (Gtk.StateType.Normal, Style.Base (Gtk.StateType.Normal));
		}
		
		public void Clear ()
		{
			foreach (Widget child in table.Children) {
				if (child != headerIcon && child != header) {
					table.Remove (child);
				}
			}
			
			table.Resize (1, 2);
		}

		protected override void OnSizeRequested (ref Gtk.Requisition req)
		{
			req = table.SizeRequest ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);

			Gtk.Requisition tableReq = table.ChildRequisition;
			allocation.X = Math.Max ((allocation.Width - tableReq.Width) / 2, 0);
			allocation.Y = Math.Max ((int)((allocation.Height - tableReq.Height) * 0.35), 0);
			allocation.Width = tableReq.Width;
			allocation.Height = tableReq.Height;
			table.SizeAllocate (allocation);
		}
	}
}

