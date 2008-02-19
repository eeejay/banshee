//
// BansheeIconFactory.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Reflection;

using Gtk;
using Gdk;

namespace Banshee.Gui
{
    public class BansheeIconFactory : IconFactory
    {
         private static string [] stock_icon_names = {
            /* Playback Control Icons */
            "media-skip-forward",
            "media-skip-backward",
            "media-playback-start",
            "media-playback-pause",
            "media-playback-stop",
            "media-eject",
            
            /* Volume Button Icons */
            "audio-volume-high", 
            "audio-volume-medium",
            "audio-volume-low", 
            "audio-volume-muted",
            "audio-volume-decrease",
            "audio-volume-increase",

            /* Other */
            "cd-action-burn",
            "cd-action-rip",
            
            /* Emotes */
            "face-smile",
            "face-sad"
        };    

        public BansheeIconFactory ()
        {
            AddDefault ();
            Initialize ();
        }
        
        private void AddResourceToIconSet (Assembly asm, string stockId, int size, 
            IconSize iconSize, IconSet iconSet)
        {
            string resource_name = stockId + "-" + size.ToString () + ".png";
            
            if(asm.GetManifestResourceInfo (resource_name) == null) {
                return;
            }
            
            IconSource source = new IconSource ();
            source.Pixbuf = new Pixbuf (asm, resource_name);
            source.Size = iconSize;
            iconSet.AddSource (source);
        }
        
        public void AddThemeIcon (string iconName)
        {
            StockItem item = new StockItem (iconName, null, 0, Gdk.ModifierType.ShiftMask, null);
            IconSet icon_set = new IconSet ();
            
            AddThemeIconToIconSet (iconName, IconSize.Menu, icon_set);
            AddThemeIconToIconSet (iconName, IconSize.SmallToolbar, icon_set);
            AddThemeIconToIconSet (iconName, IconSize.Dialog, icon_set);
            
            Add (iconName, icon_set);
            StockManager.Add (item);
        }
        
        private void AddThemeIconToIconSet (string stockId, IconSize iconSize, IconSet iconSet)
        {
            try {
                IconSource source = new IconSource ();
                source.IconName = stockId;
                source.Size = iconSize;
                iconSet.AddSource (source);
            } catch(Exception) {
            }
        }

        private void Initialize ()
        {
            Assembly asm = Assembly.GetExecutingAssembly ();

            foreach (string item_id in stock_icon_names) {
                StockItem item = new StockItem (item_id, null, 0, Gdk.ModifierType.ShiftMask, null);
                
                IconSet icon_set = null; 
                
                if (IconThemeUtils.HasIcon (item.StockId)) {
                    // map available icons from the icon theme to stock 
                    icon_set = new IconSet ();
                    AddThemeIconToIconSet (item.StockId, IconSize.Menu, icon_set);
                    AddThemeIconToIconSet (item.StockId, IconSize.SmallToolbar, icon_set);
                    AddThemeIconToIconSet (item.StockId, IconSize.Dialog, icon_set);
                } else {
                    // icon wasn't available in the theme, try to load it as stock from a resource file
                    Pixbuf default_pixbuf = null;
                    
                    foreach (string postfix in new string [] { "", "-16", "-24", "-48" }) {
                        string resource_name = item.StockId + postfix + ".png";
                        if (asm.GetManifestResourceInfo (resource_name) == null) {
                            continue;
                        }
                        
                        try {
                            default_pixbuf = new Pixbuf (asm, resource_name);
                        } catch {
                        }
                    }
                    
                    if (default_pixbuf == null) {
                        continue;
                    }
                    
                    icon_set = new IconSet (default_pixbuf);
                    AddResourceToIconSet (asm, item.StockId, 16, IconSize.Menu, icon_set);
                    AddResourceToIconSet (asm, item.StockId, 24, IconSize.SmallToolbar, icon_set);
                    AddResourceToIconSet (asm, item.StockId, 48, IconSize.Dialog, icon_set); 
                }
                
                if (icon_set == null) {
                    continue;
                }
                
                Add (item.StockId, icon_set);
                StockManager.Add (item);
            }
        }
    }
}
