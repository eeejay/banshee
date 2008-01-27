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

namespace Banshee.Sources.Gui
{
    internal class SourceRowRenderer : CellRendererText
    {
        public bool Selected = false;
        public bool Italicized = false;
        public Source source;
        public SourceView view;

        public SourceRowRenderer()
        {
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
            int titleLayoutWidth, titleLayoutHeight;
            int countLayoutWidth, countLayoutHeight;
            int maxTitleLayoutWidth;
            
            if(source == null) {
                return;
            }
            
            bool hideCounts = source.Count <= 0;
            
            StateType state = RendererStateToWidgetState(flags);
            Pixbuf icon = null;
            
            if(source == null) {
                return;
            }

            icon = source.Properties.Get<Gdk.Pixbuf>("IconPixbuf");
            if(icon == null) {
                Type icon_type = source.Properties.GetType("IconName");
                
                if(icon_type == typeof(string)) {
                    icon = Banshee.Gui.IconThemeUtils.LoadIcon(22, source.Properties.GetString("IconName"));
                } else if(icon_type == typeof(string [])) {
                    icon = Banshee.Gui.IconThemeUtils.LoadIcon(22, source.Properties.GetStringList("IconName"));
                }
                
                if(icon == null) {
                    icon = Banshee.Gui.IconThemeUtils.LoadIcon(22, "image-missing");
                }
                
                if(icon != null) {
                    source.Properties.Set<Gdk.Pixbuf>("IconPixbuf", icon);
                }
            }
            
            Pango.Layout titleLayout = new Pango.Layout(widget.PangoContext);
            Pango.Layout countLayout = new Pango.Layout(widget.PangoContext);
            
            FontDescription fd = widget.PangoContext.FontDescription.Copy();
            fd.Weight = Selected ? Pango.Weight.Bold : Pango.Weight.Normal;
            if(Italicized/* || source.HasEmphasis*/) {
                fd.Style = Pango.Style.Italic;
                hideCounts = true;
            }

            titleLayout.FontDescription = fd;
            countLayout.FontDescription = fd;
            
            titleLayout.SetMarkup("...");
            titleLayout.GetPixelSize(out titleLayoutWidth, out titleLayoutHeight);
            int ellipsisSize = titleLayoutWidth;
            
            string titleText = source.Name;
            titleLayout.SetMarkup(GLib.Markup.EscapeText(titleText));
            countLayout.SetMarkup("<span size=\"small\">(" + source.Count + ")</span>");
            
            titleLayout.GetPixelSize(out titleLayoutWidth, out titleLayoutHeight);
            countLayout.GetPixelSize(out countLayoutWidth, out countLayoutHeight);
            
            maxTitleLayoutWidth = cell_area.Width - (icon == null ? 0 : icon.Width) - countLayoutWidth - 10;
            
            if(titleLayoutWidth > maxTitleLayoutWidth) {
                float ratio = (float)(maxTitleLayoutWidth - ellipsisSize) / (float)titleLayoutWidth;
                int characters = (int)(ratio * (float)titleText.Length);
                do {
                    if(characters > 0) {
                        titleLayout.SetMarkup(GLib.Markup.EscapeText(
                            titleText.Substring(0, characters--)).Trim() + "...");
                        titleLayout.GetPixelSize(out titleLayoutWidth, out titleLayoutHeight);
                    } else {
                        hideCounts = true;
                        titleLayout.SetMarkup(GLib.Markup.EscapeText(titleText.Trim()));
                        break;
                    }
                } while (titleLayoutWidth > maxTitleLayoutWidth);
            }
            
            Gdk.GC mainGC = widget.Style.TextGC(state);

            if(icon != null) {
                drawable.DrawPixbuf(mainGC, icon, 0, 0, 
                    cell_area.X + 0, 
                    cell_area.Y + ((cell_area.Height - icon.Height) / 2),
                    icon.Width, icon.Height,
                    RgbDither.None, 0, 0);
            }
            
            drawable.DrawLayout(mainGC, 
                cell_area.X + (icon == null ? 0 : icon.Width) + 6, 
                cell_area.Y + ((cell_area.Height - titleLayoutHeight) / 2) + 1, 
                titleLayout);
            
            if(hideCounts) {
                return;
            }
                
            Gdk.GC modGC = widget.Style.TextGC(state);
            if(!state.Equals(StateType.Selected)) {
                modGC = new Gdk.GC(drawable);
                modGC.Copy(widget.Style.TextGC(state));
                Gdk.Color fgcolor = widget.Style.Foreground(state);
                Gdk.Color bgcolor = widget.Style.Background(state);
                modGC.RgbFgColor = Hyena.Gui.GtkUtilities.ColorBlend(fgcolor, bgcolor);
                modGC.RgbBgColor = fgcolor;
            } 
            
            drawable.DrawLayout(modGC,
                (cell_area.X + cell_area.Width) - countLayoutWidth - 2,
                cell_area.Y + ((cell_area.Height - countLayoutHeight) / 2) + 1,
                countLayout);
        }
        
        public override CellEditable StartEditing(Gdk.Event evnt , Widget widget, 
            string path, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, 
            CellRendererState flags)
        {
            CellEditEntry text = new CellEditEntry();
            text.EditingDone += OnEditDone;
            text.Text = source.Name;
            text.path = path;
            text.Show();
            
            view.EditingRow = true;
            
            return text;
        }
        
        private void OnEditDone(object o, EventArgs args)
        {
            CellEditEntry edit = (CellEditEntry)o;
            if(view == null) {
                return;
            }
            
            view.EditingRow = false;
            view.UpdateRow(new TreePath(edit.path), edit.Text);
        }
    }
}
