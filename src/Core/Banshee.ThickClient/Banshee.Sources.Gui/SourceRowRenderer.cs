//
// SourceRowRenderer.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;
using Gdk;
using Pango;

using Hyena.Data.Gui;
using Hyena.Gui.Theatrics;

using Banshee.ServiceStack;

namespace Banshee.Sources.Gui
{
    internal class SourceRowRenderer : CellRendererText
    {
        public bool Selected = false;
        public bool Italicized = false;
        public Source source;
        public SourceView view;
        public TreePath path;

        public SourceRowRenderer ()
        {
        }
        
        private StateType RendererStateToWidgetState (CellRendererState flags)
        {
            return (CellRendererState.Selected & flags).Equals (CellRendererState.Selected)
                ? StateType.Selected
                : StateType.Normal;
        }
        
        public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area,
            out int x_offset, out int y_offset, out int width, out int height)
        {        
            int text_x, text_y, text_w, text_h;
   
            base.GetSize (widget, ref cell_area, out text_x, out text_y, out text_w, out text_h);
                
            x_offset = 0;
            y_offset = 0;
            width = text_w;
            height = text_h + 5;
        }
        
        protected override void Render (Gdk.Drawable drawable, Widget widget, Gdk.Rectangle background_area, 
            Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
        {
            if (source == null) {
                return;
            }
            
            bool path_selected = view.Selection.PathIsSelected (path);            
            StateType state = RendererStateToWidgetState (flags);
            
            if (path_selected) {
                Gdk.Rectangle rect = background_area;
                rect.X -= 2;
                rect.Width += 4;
                
                // clear the standard GTK selection and focus
                drawable.DrawRectangle (widget.Style.BaseGC (StateType.Normal), true, rect);
                
                // draw the hot cairo selection
                if (!view.EditingRow) { 
                    view.Graphics.DrawRowSelection (view.Cr, background_area.X + 1, background_area.Y + 1, 
                        background_area.Width - 2, background_area.Height - 2);
                }
            } else if (path != null && path.Equals (view.HighlightedPath)) {
                view.Graphics.DrawRowSelection (view.Cr, background_area.X + 1, background_area.Y + 1, 
                    background_area.Width - 2, background_area.Height - 2, false);
            } else if (view.NotifyStage.ActorCount > 0) {
                TreeIter iter;
                if (view.Model.GetIter (out iter, path) && view.NotifyStage.Contains (iter)) {
                    Actor<TreeIter> actor = view.NotifyStage[iter];
                    Cairo.Color color = view.Graphics.GetWidgetColor (GtkColorClass.Background, StateType.Selected);
                    color.A = 1.0 - actor.Percent; 
                    
                    view.Graphics.DrawFlatRowHighlight (view.Cr, background_area.X + 1, background_area.Y + 1, 
                        background_area.Width - 2, background_area.Height - 2, color);
                }
            }
            
            int title_layout_width = 0, title_layout_height = 0;
            int count_layout_width = 0, count_layout_height = 0;
            int max_title_layout_width;
            
            bool hide_counts = source.Count <= 0;
            
            Pixbuf icon = ResolveSourceIcon (source);
            
            FontDescription fd = widget.PangoContext.FontDescription.Copy ();
            fd.Weight = (ISource)ServiceManager.PlaybackController.Source == (ISource)source 
                ? Pango.Weight.Bold 
                : Pango.Weight.Normal;

            if (Italicized) {
                fd.Style = Pango.Style.Italic;
                hide_counts = true;
            }
            
            Pango.Layout title_layout = new Pango.Layout (widget.PangoContext);
            Pango.Layout count_layout = null;
            
            if (!hide_counts) {
                count_layout = new Pango.Layout (widget.PangoContext);
                count_layout.FontDescription = fd;
                count_layout.SetMarkup (String.Format ("<span size=\"small\">({0})</span>", source.Count));
                count_layout.GetPixelSize (out count_layout_width, out count_layout_height);
            }

            max_title_layout_width = cell_area.Width - (icon == null ? 0 : icon.Width) - count_layout_width - 10;
            
            title_layout.FontDescription = fd;
            title_layout.Width = (int)(max_title_layout_width * Pango.Scale.PangoScale);
            title_layout.Ellipsize = EllipsizeMode.End;
            title_layout.SetText (source.Name);
            title_layout.GetPixelSize (out title_layout_width, out title_layout_height);
            
            Gdk.GC main_gc = widget.Style.TextGC (state);

            if (icon != null) {
                drawable.DrawPixbuf (main_gc, icon, 0, 0, 
                    cell_area.X + 0, cell_area.Y + ((cell_area.Height - icon.Height) / 2) + 1,
                    icon.Width, icon.Height, RgbDither.None, 0, 0);
            }
            
            drawable.DrawLayout (main_gc, 
                cell_area.X + (icon == null ? 0 : icon.Width) + 6, 
                cell_area.Y + ((cell_area.Height - title_layout_height) / 2) + 1, 
                title_layout);
            
            if (hide_counts) {
                return;
            }
                
            Gdk.GC mod_gc = widget.Style.TextGC (state);
            if (!state.Equals (StateType.Selected)) {
                Gdk.Color fgcolor = widget.Style.Foreground (state);
                Gdk.Color bgcolor = widget.Style.Background (state);
                
                mod_gc = new Gdk.GC (drawable);
                mod_gc.Copy (widget.Style.TextGC (state));
                mod_gc.RgbFgColor = Hyena.Gui.GtkUtilities.ColorBlend (fgcolor, bgcolor);
                mod_gc.RgbBgColor = fgcolor;
            } 
            
            drawable.DrawLayout (mod_gc,
                (cell_area.X + cell_area.Width) - count_layout_width - 2,
                cell_area.Y + ((cell_area.Height - count_layout_height) / 2) + 1,
                count_layout);
        }
        
        private Gdk.Pixbuf ResolveSourceIcon (Source source)
        {
            Hyena.Data.PropertyStore properties = source.Properties;
            Gdk.Pixbuf icon = source.Properties.Get<Gdk.Pixbuf> ("IconPixbuf");
            
            if (icon != null) {
                return icon;
            }
            
            Type icon_type = properties.GetType ("IconName");
                
            if(icon_type == typeof (string)) {
                icon = Banshee.Gui.IconThemeUtils.LoadIcon (22, properties.GetString ("IconName"));
            } else if (icon_type == typeof (string [])) {
                icon = Banshee.Gui.IconThemeUtils.LoadIcon (22, properties.GetStringList ("IconName"));
            }
                
            if (icon == null) {
                icon = Banshee.Gui.IconThemeUtils.LoadIcon (22, "image-missing");
            }
                
            if (icon != null) {
                properties.Set<Gdk.Pixbuf> ("IconPixbuf", icon);
            }
            
            return icon;
        }
        
        public override CellEditable StartEditing (Gdk.Event evnt , Widget widget, string path, 
            Gdk.Rectangle background_area, Gdk.Rectangle cell_area, CellRendererState flags)
        {
            CellEditEntry text = new CellEditEntry ();
            text.EditingDone += OnEditDone;
            text.Text = source.Name;
            text.path = path;
            text.Show ();
            
            view.EditingRow = true;
            
            return text;
        }
        
        private void OnEditDone (object o, EventArgs args)
        {
            CellEditEntry edit = (CellEditEntry)o;
            if (view == null) {
                return;
            }
            
            view.EditingRow = false;
            view.UpdateRow (new TreePath (edit.path), edit.Text);
        }
    }
}
