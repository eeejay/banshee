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
using System.Runtime.InteropServices;

using Banshee.Base;

namespace Banshee
{
    public class AboutBox
    {
        private static string [] Authors = {
            "Aaron Bockover",
            "Dan Winship",
            "Hans Petter Jansson",
            "James Willcox",
            "Chris Lahey",
            "Ben Maurer",
            "Larry Ewing",
            "Miguel de Icaza",
            "Aydemir Ula\u015f \u015eahin",
            "Do\u011facan G\u00fcney"
        };
        
        private static string [] Artists = {
            "Garrett LeSage",
            "Jakub Steiner",
            "Ryan Collier"
        };
        
        private static string Copyright = Catalog.GetString(
            "Copyright 2005 Novell, Inc.\n" + 
             "Copyright 2005 Aaron Bockover"); 
    
        private static string Name = Catalog.GetString("Banshee");
        
        private static string Comments = Catalog.GetString("Music Management and Playback for Gnome.");
    
        private Translator [] Translators = {
            new Translator("Jordi Mas", "Catalan"),
            new Translator("Alexander Shopov, Rostislav Raykov", "Bulgarian"),
            new Translator("Adam Weinberger", "Canadian English"),
            new Translator("Francisco Javier F. Serrador", "Spanish"),
            new Translator("Takeshi AIHANA", "Japanese"),
            new Translator("\u017Dygimantas Beru\u010Dka", "Lithuanian"),
            new Translator("Funda Wang", "Simplified Chinese"),
            new Translator("Vincent van Adrighem", "Dutch"),
            new Translator("\u00D8ivind Hoel", "Norwegian Bokm\u00E5l"),
            new Translator("Marco Carvalho", "Brazilian Portuguese"),
			new Translator("Christian Rose", "Swedish")
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
                string str = String.Empty;
                Array.Sort(translators);
                
                for(int i = 0; i < translators.Length; i++) {
                    str += translators[i].ToString() + "\n";
                }
                
                return str;
            }
        }
    
        public AboutBox()
        {
            Array.Sort(Authors);
            Array.Sort(Artists);
            
            try {
                GtkAboutDialog();
            } catch(Exception) {
                GnomeAboutDialog();
            }
        }
        
        private void GtkAboutDialog()
        {
            GtkSharpBackports.AboutDialog about_dialog = new GtkSharpBackports.AboutDialog();
            about_dialog.Name = Name;
            about_dialog.Version = ConfigureDefines.VERSION;
            about_dialog.Copyright = Copyright;
            about_dialog.Comments = Comments;
            about_dialog.Website = "http://banshee-project.org/";
            about_dialog.WebsiteLabel = Catalog.GetString("Banshee Wiki");
            about_dialog.Authors = Authors;
            about_dialog.TranslatorCredits = Translator.ToString(Translators);
            about_dialog.Artists = Artists;
            about_dialog.Icon = ThemeIcons.WindowManager;
            about_dialog.Logo = Gdk.Pixbuf.LoadFromResource("banshee-logo.png");
            about_dialog.License = Resource.GetFileContents("COPYING");
            about_dialog.WrapLicense = true;
            about_dialog.Run();
            about_dialog.Destroy();
        }
        
#pragma warning disable 0612
        private void GnomeAboutDialog()
        {
            Gnome.About about_dialog = new Gnome.About(
                Name,
                ConfigureDefines.VERSION,
                Copyright,
                Comments,
                Authors,
                null,
                Translator.ToString(Translators),
                Gdk.Pixbuf.LoadFromResource("banshee-logo.png"));
            about_dialog.Run();
            about_dialog.Destroy();
        }
#pragma warning restore 0612

    }
}
