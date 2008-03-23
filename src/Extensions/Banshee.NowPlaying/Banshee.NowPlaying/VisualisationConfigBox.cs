//
// VisualisationConfigBox.cs
//
// Author:
//   Alexander Hixon <hixon.alexander@mediati.org>
//
// Copyright (C) 2008 Alexander Hixon
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
using Gtk;

using Banshee.ServiceStack;

namespace Banshee.NowPlaying
{
    public class VisualisationConfigBox : HBox
    {
        private int height;
        private int width;
        private int fps;
        private string vis_name;
        private int vis_id;
        
        public string ActiveVisualisationName {
            get { return vis_name; }
        }
        
        public int ActiveVisualisation {
            get { return vis_id; }
        }
        
        // These match both Totem and Rhythmbox.
        private int [][] size_matrix = {
            new int [] { 200, 150, 10 }, /* small */
            new int [] { 320, 240, 20 }, /* normal */
            new int [] { 640, 480, 25 }, /* large */
            new int [] { 800, 600, 30 }  /* extra large */
        };
        
        private int active_size = 1;
        public int ActiveSize {
            get { return active_size; }
        }
        
        private string [] visualisations;
        public string [] Visualisations {
            get { return visualisations; }
        }
        
        public VisualisationConfigBox ()
        {
            vis_id = 0;
            UpdateQuality ();
            
            visualisations = ServiceManager.PlayerEngine.ActiveEngine.Visualisations;
            if (visualisations != null && visualisations.Length > 0) {
                BuildBox ();
                vis_name = visualisations [vis_id];
            }
        }
        
        public VisualisationConfigBox (VisualisationConfigBox box)
        {
            active_size = box.ActiveSize;
            vis_id = box.ActiveVisualisation;
            vis_name = box.ActiveVisualisationName;
            visualisations = box.Visualisations;
            
            UpdateQuality ();
            
            if (visualisations != null && visualisations.Length > 0) {
                BuildBox ();
            }
        }
        
        private void BuildBox ()
        {
            Label vis_label = new Label (Catalog.GetString ("Visualisation:"));
            vis_label.Show ();
            
            ComboBox vis_selection = ComboBox.NewText ();
            foreach (string visualisation in visualisations) {
                vis_selection.AppendText (visualisation);
            }
            
            vis_selection.Active = vis_id;
            vis_selection.Changed += OnVisualisationChanged;
            vis_selection.Show ();
            
            Label quality_label = new Label (Catalog.GetString ("Quality:"));
            quality_label.Show ();
            
            ComboBox quality_selection = ComboBox.NewText ();
            quality_selection.AppendText (Catalog.GetString ("Small"));
            quality_selection.AppendText (Catalog.GetString ("Normal"));
            quality_selection.AppendText (Catalog.GetString ("Large"));
            quality_selection.AppendText (Catalog.GetString ("Extra Large"));
            quality_selection.Active = active_size;
            quality_selection.Changed += OnQualityChanged;
            quality_selection.Show ();
            
            Button fullscreen = new Button ("gtk-fullscreen");
            fullscreen.Show ();
            
            PackStart (vis_label, false, false, 6);
            PackStart (vis_selection, true, true, 6);
            PackStart (quality_label, false, false, 0);
            PackStart (quality_selection, true, true, 6);
            PackStart (fullscreen, false, false, 6);
        }
        
#region Callbacks
        private void OnVisualisationChanged (object o, EventArgs args)
        {
            vis_id = (o as ComboBox).Active;
            vis_name = visualisations [vis_id];
            ServiceManager.PlayerEngine.ActiveEngine.SetCurrentVisualisation (vis_name, width, height, fps);
        }
        
        private void OnQualityChanged (object o, EventArgs args)
        {
            int idx = (o as ComboBox).Active;
            active_size = idx;
            UpdateQuality ();
            
            ServiceManager.PlayerEngine.ActiveEngine.SetCurrentVisualisation (vis_name, width, height, fps);
        }
        
        private void UpdateQuality ()
        {
            width = size_matrix [active_size][0];
            height = size_matrix [active_size][1];
            fps = size_matrix [active_size][2];
        }
#endregion
    }
}
