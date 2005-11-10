/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  StockIcons.cs
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
using Gtk;
using Gdk;
using Mono.Unix;
using System.Collections;

namespace Banshee
{
    public class StockIcons 
    {
        static StockItem FromDef(string id, string label, uint keyval, 
            ModifierType modifier, string domain)
        {
            StockItem item;
            item.StockId = id;
            item.Label = label;
            item.Keyval = keyval;
            item.Modifier = modifier;
            item.TranslationDomain = domain;
            return item;
        }
        
        private static StockItem [] stock_items = {
            /* Playback Control Icons */
            FromDef("media-next", Catalog.GetString("Next"), 0, ModifierType.ShiftMask, null),
            FromDef("media-prev", Catalog.GetString("Previous"), 0, ModifierType.ShiftMask, null),
            FromDef("media-play", Catalog.GetString("Play"), 0, ModifierType.ShiftMask, null),
            FromDef("media-pause", Catalog.GetString("Pause"), 0, ModifierType.ShiftMask, null),
            FromDef("media-shuffle", Catalog.GetString("Shuffle"), 0, ModifierType.ShiftMask, null),
            FromDef("media-repeat", Catalog.GetString("Repeat"), 0, ModifierType.ShiftMask, null),
            FromDef("media-eject", Catalog.GetString("Eject"), 0, ModifierType.ShiftMask, null),
            
            /* Volume Button Icons */
            FromDef("volume-max", Catalog.GetString("Volume Maximum"), 0, ModifierType.ShiftMask, null),
            FromDef("volume-med", Catalog.GetString("Volume Medium"), 0, ModifierType.ShiftMask, null),
            FromDef("volume-min", Catalog.GetString("Volume Miniumum"), 0, ModifierType.ShiftMask, null),
            FromDef("volume-zero", Catalog.GetString("Volume Mute"), 0, ModifierType.ShiftMask, null),
            FromDef("volume-decrease", Catalog.GetString("Volume Decrease"), 0, ModifierType.ShiftMask, null),
            FromDef("volume-increase", Catalog.GetString("Volume Increase"), 0, ModifierType.ShiftMask, null),
            
            /* Now Playing Images */
            FromDef("icon-artist", Catalog.GetString("Artist"), 0, ModifierType.ShiftMask, null),
            FromDef("icon-album", Catalog.GetString("Album"), 0, ModifierType.ShiftMask, null),
            FromDef("icon-title", Catalog.GetString("Title"), 0, ModifierType.ShiftMask, null),
            
            /* Other */
            FromDef("media-burn", Catalog.GetString("Write CD"), 0, ModifierType.ShiftMask, null),
            FromDef("media-rip", Catalog.GetString("Import CD"), 0, ModifierType.ShiftMask, null)
        };    

        public static void Initialize()
        {
            IconFactory icon_factory = new IconFactory();
            icon_factory.AddDefault();
            
            Hashtable map = new Hashtable();
            foreach(StockItem item in stock_items) {
                map[item.StockId] = item.StockId;
            }
            
            map["media-rip"] = "cd-action-rip-24";
            map["media-burn"] = "cd-action-burn-24";

            foreach(StockItem item in stock_items) {
                Pixbuf pixbuf = Pixbuf.LoadFromResource((map[item.StockId] as string) + ".png");
                IconSet icon_set = new IconSet(pixbuf);
                icon_factory.Add(item.StockId, icon_set);
                StockManager.Add(item);
            }
        }
    }
}
