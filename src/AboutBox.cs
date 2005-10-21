/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  AboutBox.cs
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
using Mono.Unix;

namespace Banshee
{
    public class AboutBox
    {
        private static string [] Authors = {
            "Aaron Bockover",
            "Dan Winship",
            "Hans Petter Jansson",
            "James Wilcox",
            "Chris Lahey",
            "Ben Maurer",
            "Larry Ewing"
        };
    
        private Translator [] Translators = {
            new Translator("Jordi Mas", "Catalan"),
            new Translator("Alexander Shopov, Rostislav Raykov", "Bulgarian"),
            new Translator("Adam Weinberger", "Canadian English"),
            new Translator("Francisco Javier F. Serrador", "Spanish"),
            new Translator("Takeshi AIHANA", "Japanese"),
            new Translator("\u017Dygimantas Beru\u010Dka", "Lithuanian"),
            new Translator("Funda Wang", "Simplified Chinese"),
            new Translator("Vincent van Adrighem", "Dutch")
        };
    
        private class Translator : IComparable
        {
            public Translator(string name, string language)
            {
                Name = name;
                Language = language;
            }
            
            public string Name;
            public string Language;
        
            public override string ToString()
            {
                return String.Format("{0} ({1})", Name, Language);
            }
            
            public int CompareTo(object o)
            {
                if(!(o is Translator)) {
                    throw new ArgumentException("Object must be Translator");
                }    
                
                return -(o as Translator).Name.CompareTo(Name);
            }
            
            public static string ToString(Translator [] translators)
            {
                string ret = String.Empty;

                Array.Sort(translators);
                
                foreach(Translator translator in translators) {
                    ret += translator + "\n";
                }
                
                return ret;
            }
        }
    
        public AboutBox()
        {
            Array.Sort(Authors);
            
            Gnome.About about = new Gnome.About(
                "Banshee", 
                ConfigureDefines.VERSION,
                Catalog.GetString(
                    "Copyright 2005 Novell, Inc.\n" + 
                    "Copyright 2005 Aaron Bockover"),
                String.Format(Catalog.GetString(
                    "Music Management and Playback for Gnome.\n" + 
                    "Released under the MIT license.\n" + 
                    "See the Banshee Wiki at {0}"), 
                    "www.banshee-project.org"),
                Authors,
                null,
                Translator.ToString(Translators),
                Gdk.Pixbuf.LoadFromResource("banshee-logo.png")
            );
            
            about.Icon = ThemeIcons.WindowManager;
            about.Run();
        }
    }
}
