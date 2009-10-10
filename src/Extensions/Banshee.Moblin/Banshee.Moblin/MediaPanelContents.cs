// 
// MediaPanelContents.cs
//  
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2009 Novell, Inc.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Gtk;

using Hyena.Data.Gui;
using Banshee.Collection.Gui;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.PlayQueue;

namespace Banshee.Moblin
{
    public class MediaPanelContents : Table
    {
        private TerseTrackListView playqueue_view;
        
        public MediaPanelContents () : base (2, 2, false)
        {
            BuildViews ();
            FindPlayQueue ();
        }
        
        private void BuildViews ()
        {
            Attach (new Hyena.Widgets.ScrolledWindow () {
                (playqueue_view = new TerseTrackListView () {
                    HasFocus = true
                })
            }, 1, 2, 1, 2, AttachOptions.Shrink,
                AttachOptions.Expand | AttachOptions.Fill, 0, 0);
            
            playqueue_view.SetSizeRequest (425, -1);
            playqueue_view.ColumnController.Insert (new Column (null, "indicator",
                new ColumnCellStatusIndicator (null), 0.05, true, 20, 20), 0);
            
            ShowAll ();
        }

#region PlayQueue

        private void FindPlayQueue ()
        {
            Banshee.ServiceStack.ServiceManager.SourceManager.SourceAdded += delegate (SourceAddedArgs args) {
                if (args.Source is Banshee.PlayQueue.PlayQueueSource) {
                    InitPlayQueue (args.Source as Banshee.PlayQueue.PlayQueueSource);
                }
            };

            foreach (Source src in ServiceManager.SourceManager.Sources) {
                if (src is Banshee.PlayQueue.PlayQueueSource) {
                    InitPlayQueue (src as Banshee.PlayQueue.PlayQueueSource);
                }
            }
        }

        private void InitPlayQueue (PlayQueueSource play_queue)
        {
            ServiceManager.SourceManager.SetActiveSource (play_queue);
            //play_queue.TrackModel.Reloaded += HandleTrackModelReloaded;
            playqueue_view.SetModel (play_queue.TrackModel);
        }

#endregion

    }
}