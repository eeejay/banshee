//
// BetaReleaseViewOverlay.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using Mono.Unix;
using Cairo;
using Gtk;

using Hyena.Gui;
using Hyena.Gui.Theming;
using Hyena.Gui.Theatrics;

using Banshee.Gui;
using Banshee.Sources;
using Banshee.ServiceStack;

namespace Banshee.Collection.Gui
{
    public class BetaReleaseViewOverlay
    {
        private string welcome_string;
        private SingleActorStage stage = new SingleActorStage ();
        private Gdk.Pixbuf logo_scale;
        private Gdk.Pixbuf arrow;
        private Widget widget;
        private Source source;
        private bool dismissed;

        public event EventHandler Finished;

        public BetaReleaseViewOverlay (Widget widget)
        {
            if (dismissed) {
                return;
            }

            this.widget = widget;

            System.Text.StringBuilder builder = new System.Text.StringBuilder ();
            builder.AppendFormat ("<big><big><big><b>{0}</b></big></big></big>\n\n", GLib.Markup.EscapeText (
                Catalog.GetString ("Welcome to the Banshee 1.0 Alpha 1 release!")));
            builder.Append (Catalog.GetString (
                "It is <i>very</i> important to note that this is a <i>preview release</i> and does " +
                "<i>not yet</i> contain all of the features you may be used to in previous Banshee releases."));
            builder.Append ("\n\n");
            builder.Append (Catalog.GetString (
                "Most notably, hardware features are not yet available (Audio CDs, Digital Audio Players), " + 
                "and the Podcasting, Internet Radio, Recommendations, Mini Mode, and DAAP (iTunes Music Sharing) " +
                "plugins are not available."));
            builder.Append ("\n\n");
            builder.Append (Catalog.GetString (
                "All of the features you have come to love in Banshee will be added back before the final 1.0 release."));
            builder.Append ("\n\n");
            builder.AppendFormat ("<big><b><i>{0}</i></b></big>", Catalog.GetString ("Enjoy the preview!"));
            welcome_string = builder.ToString ();

            source = ServiceManager.SourceManager.DefaultSource;
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;
        }

        private void OnActiveSourceChanged (SourceEventArgs args)
        {
            if (source != null && source != args.Source) {
                if (source.Properties.GetString ("Message.Id") == "beta-release") {
                    source.Properties.RemoveStartingWith ("Message.");
                }
            }

            source = args.Source;

            if (!source.Properties.Contains ("Message.Text") && ! source.Properties.Contains ("Message.Id")) {
                source.Properties.SetString ("Message.Id", "beta-release");
                source.Properties.SetString ("Message.Text", Catalog.GetString (
                    "Please confirm your understanding of the message above."
                ));
                source.Properties.SetString ("Message.Icon.Name", Stock.Info);
                source.Properties.SetBoolean ("Message.CanClose", false);
                source.Properties.SetString ("Message.Action.Label", 
                    Catalog.GetString ("I understand this is a preview release"));
                source.Properties.Set<EventHandler> ("Message.Action.NotifyHandler", delegate { 
                    stage.Iteration += OnStageIteration;
                    stage.Reset ();

                    ServiceManager.SourceManager.ActiveSourceChanged -= OnActiveSourceChanged;

                    if (source != null) {
                        source.Properties.RemoveStartingWith ("Message.");
                        source = null;
                    }
                    
                    dismissed = true;
                });
            }
        }

        protected virtual void OnFinished ()
        {
            EventHandler handler = Finished;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        private void OnStageIteration (object o, EventArgs args)
        {
            if (stage.ActorCount == 0) {
                stage.Iteration -= OnStageIteration;
                stage = null;

                if (logo_scale != null) {
                    logo_scale.Dispose ();
                    logo_scale = null;
                }

                OnFinished ();
            }

            widget.QueueDraw ();
        }

        public void Render (Theme theme, Gdk.Rectangle allocation, Cairo.Context cr, Gdk.Rectangle clip)
        {
            if (widget == null) {
                return;
            }

            if (logo_scale == null) {
                logo_scale = Gdk.Pixbuf.LoadFromResource ("banshee-logo.png");
                logo_scale = logo_scale.ScaleSimple (64, 64, Gdk.InterpType.Bilinear);
            }

            theme.PushContext ();
            theme.Context.Cairo = cr;
            theme.Context.Radius = 12;

            Gdk.Rectangle rect = new Gdk.Rectangle ();
            rect.Width = (int)Math.Round (allocation.Width * 0.75);

            int padding = (int)theme.Context.Radius * 2;
            int spacing = padding / 2;
            int layout_width = rect.Width - logo_scale.Width - 2 * padding - spacing;
            int layout_height;

            Pango.Layout layout = new Pango.Layout (widget.PangoContext);
            layout.FontDescription = widget.PangoContext.FontDescription.Copy ();
            layout.Width = (int)(layout_width * Pango.Scale.PangoScale);
            layout.Wrap = Pango.WrapMode.Word;
            layout.SetMarkup (welcome_string);

            layout.GetPixelSize (out layout_width, out layout_height);

            rect.Height = layout_height + 2 * padding;
            rect.X = (allocation.Width - rect.Width) / 2;
            rect.Y = (allocation.Height - rect.Height) / 2;
            int layout_x = rect.X + padding + spacing + logo_scale.Width;
            int layout_y = rect.Y + padding;
            double alpha = stage.ActorCount > 0 ? 1.0 - stage.Actor.Percent : 1.0;

            Cairo.Color color = theme.Colors.GetWidgetColor (GtkColorClass.Background, StateType.Normal);
            color.A = Theme.Clamp (0.0, 0.85, alpha);
            cr.Color = color;
            cr.Rectangle (0, 0, allocation.Width, allocation.Height);
            cr.Fill ();

            if (stage.Playing && alpha < 1.0) {
                CairoExtensions.PushGroup (cr);
            }

            theme.Context.FillAlpha = 0.65;
            theme.DrawFrame (cr, rect, true);
            cr.MoveTo (layout_x, layout_y);
            cr.Color = theme.Colors.GetWidgetColor (GtkColorClass.Text, StateType.Normal);
            Pango.CairoHelper.ShowLayout (cr, layout);

            int x = rect.X + padding;
            int y = rect.Y + padding;
            cr.Rectangle (x, y, logo_scale.Width, logo_scale.Height);
            cr.Save ();
            cr.Translate (0.5, 0.5);
            Gdk.CairoHelper.SetSourcePixbuf (cr, logo_scale, x, y);
            cr.Fill ();
            cr.Restore ();

            if (arrow == null) {
                arrow = IconThemeUtils.LoadIcon (22, Stock.GoDown);
            }

            x = rect.X + rect.Width - spacing - arrow.Width;
            y = rect.Y + rect.Height - spacing - arrow.Height;
            cr.Rectangle (x, y, arrow.Width, arrow.Height);
            cr.Save ();
            cr.Translate (0.5, 0.5);
            Gdk.CairoHelper.SetSourcePixbuf (cr, arrow, x, y);
            cr.Fill ();
            cr.Restore ();

            if (stage.Playing && alpha < 1.0) {
                CairoExtensions.PopGroupToSource (cr);
                cr.PaintWithAlpha (alpha);
            }

            theme.PopContext ();
        }
    }
}
