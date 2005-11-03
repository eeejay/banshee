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

namespace Banshee
{
    public class AboutDialog : Gtk.Dialog
    {
        protected AboutDialog(IntPtr raw) : base(raw) 
        {
        }
        
        public AboutDialog() : base(IntPtr.Zero)
        {
            CreateNativeObject(new string[0], new GLib.Value[0]);
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_get_type();
        
        public static new GLib.GType GType {
            get {
                return new GLib.GType(gtk_about_dialog_get_type());
            }
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_set_name(IntPtr raw, IntPtr name);
        
        public new string Name {
            set {
                IntPtr raw = GLib.Marshaller.StringToPtrGStrdup(value);
                gtk_about_dialog_set_name(Handle, raw);
                GLib.Marshaller.Free(raw);
            }
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_set_version(IntPtr raw, IntPtr version);
        
        public string Version {
            set {
                IntPtr raw = GLib.Marshaller.StringToPtrGStrdup(value);
                gtk_about_dialog_set_version(Handle, raw);
                GLib.Marshaller.Free(raw);
            }
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_set_copyright(IntPtr raw, IntPtr copyright);
        
        public string Copyright {   
            set {
                IntPtr raw = GLib.Marshaller.StringToPtrGStrdup(value);
                gtk_about_dialog_set_copyright(Handle, raw);
                GLib.Marshaller.Free(raw);
            }
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_set_comments(IntPtr raw, IntPtr comments);
        
        public string Comments {   
            set {
                IntPtr raw = GLib.Marshaller.StringToPtrGStrdup(value);
                gtk_about_dialog_set_comments(Handle, raw);
                GLib.Marshaller.Free(raw);
            }
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_set_license(IntPtr raw, IntPtr license);
        
        public string License {   
            set {
                IntPtr raw = GLib.Marshaller.StringToPtrGStrdup(value);
                gtk_about_dialog_set_license(Handle, raw);
                GLib.Marshaller.Free(raw);
            }
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_set_wrap_license(IntPtr raw, bool wrap);
        
        public bool WrapLicense {   
            set {
                gtk_about_dialog_set_wrap_license(Handle, value);
            }
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_set_website(IntPtr raw, IntPtr website);
        
        public string Website {   
            set {
                IntPtr raw = GLib.Marshaller.StringToPtrGStrdup(value);
                gtk_about_dialog_set_website(Handle, raw);
                GLib.Marshaller.Free(raw);
            }
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_set_website_label(IntPtr raw, IntPtr website_label);
        
        public string WebsiteLabel {   
            set {
                IntPtr raw = GLib.Marshaller.StringToPtrGStrdup(value);
                gtk_about_dialog_set_website_label(Handle, raw);
                GLib.Marshaller.Free(raw);
            }
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_set_authors(IntPtr raw, string [] authors);
        
        public string [] Authors {
            set {
               gtk_about_dialog_set_authors(Handle, value);
            }
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_set_artists(IntPtr raw, string [] artists);
        
        public string [] Artists {
            set {
               gtk_about_dialog_set_artists(Handle, value);
            }
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_set_documenters(IntPtr raw, string [] documenters);
        
        public string [] Documenters {
            set {
               gtk_about_dialog_set_documenters(Handle, value);
            }
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_set_translator_credits(IntPtr raw, IntPtr credits);
        
        public string TranslatorCredits {
            set {
                IntPtr raw = GLib.Marshaller.StringToPtrGStrdup(value);
                gtk_about_dialog_set_translator_credits(Handle, raw);
                GLib.Marshaller.Free(raw);
            }
        }
        
        [DllImport("libgtk-win32-2.0-0.dll")]
        static extern IntPtr gtk_about_dialog_set_logo(IntPtr raw, IntPtr logo);
        
        public Gdk.Pixbuf Logo {
            set {
                gtk_about_dialog_set_logo(Handle, value.Raw);
            }
        }
    }

    public class AboutBox
    {
        private static string [] Authors = {
            "Aaron Bockover",
            "Dan Winship",
            "Hans Petter Jansson",
            "James Wilcox",
            "Chris Lahey",
            "Ben Maurer",
            "Larry Ewing",
            "Miguel de Icaza"
        };
        
        private static string [] Artists = {
            "Garrett LeSage",
            "Jakub Steiner",
            "Ryan Collier"
        };
    
        private Translator [] Translators = {
            new Translator("Jordi Mas", "Catalan"),
            new Translator("Alexander Shopov, Rostislav Raykov", "Bulgarian"),
            new Translator("Adam Weinberger", "Canadian English"),
            new Translator("Francisco Javier F. Serrador", "Spanish"),
            new Translator("Takeshi AIHANA", "Japanese"),
            new Translator("\u017Dygimantas Beru\u010Dka", "Lithuanian"),
            new Translator("Funda Wang", "Simplified Chinese"),
            new Translator("Vincent van Adrighem", "Dutch"),
            new Translator("Norwegian Bokm\00E5l", "\u00D8ivind Hoel")
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
            
            AboutDialog about_dialog = new AboutDialog();
            about_dialog.Name = "Banshee";
            about_dialog.Version = ConfigureDefines.VERSION;
            about_dialog.Copyright = Catalog.GetString(
                "Copyright 2005 Novell, Inc.\n" + 
                "Copyright 2005 Aaron Bockover");
            about_dialog.Comments = Catalog.GetString("Music Management and Playback for Gnome.");
            about_dialog.Website = "http://banshee-project.org/";
            about_dialog.WebsiteLabel = "Banshee Wiki";
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
    }
}
