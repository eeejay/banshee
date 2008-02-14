//
// AboutDialog.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2007 Novell, Inc.
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
using System.Text;
using System.Collections.Generic;

using Gtk;
using Mono.Unix;

using Hyena;
using Banshee.Base;

namespace Banshee.Gui.Dialogs
{
    public class AboutDialog : Gtk.AboutDialog
    {
        public AboutDialog() : base()
        {
            // build authors page
            List<string> authors = new List<string> ();            
            authors.Add (Catalog.GetString ("Primary Development"));
            
            foreach (ProductAuthor author in ProductInformation.Authors) {
                authors.Add (String.Format("    {0} ({1})", author.Name, author.Role));
            }
            
            authors.Add ("");
            authors.Add (Catalog.GetString("Contributors"));
            
            foreach (string author in ProductInformation.Contributors) {
                authors.Add (String.Format("    {0}", author));
            }
            
            authors.Add ("");
            
            // build translators page
            StringBuilder translation_credits = new StringBuilder ();
            
            foreach (ProductTranslation translation in ProductInformation.Translations) {
                translation_credits.Append (String.Format ("{0}\n", translation.LanguageName));
                foreach (string person in translation.Translators) {
                    translation_credits.Append (String.Format ("    {0}\n", person));
                }
                translation_credits.Append ("\n");
            }
            
            //Logo = Branding.AboutBoxLogo;
            //Name = Branding.ApplicationName;
            Name = "Banshee";
            Logo = Gdk.Pixbuf.LoadFromResource ("banshee-logo.png");
            Version = Banshee.ServiceStack.Application.Version;
            Comments = Catalog.GetString ("Music Management and Playback for GNOME.");
            Copyright = Catalog.GetString (
                "Copyright \u00a9 2005-2007 Novell, Inc.\n" + 
                "Copyright \u00a9 2005 Aaron Bockover"
            );
            
            Website = "http://banshee-project.org/";
            WebsiteLabel = Catalog.GetString ("Banshee Wiki");

            Authors = authors.ToArray ();
            Artists = ProductInformation.Artists;
            TranslatorCredits = translation_credits.ToString ();
            
            License = ProductInformation.License;
            WrapLicense = true;
            
            //SetUrlHook(delegate(Gtk.AboutDialog dialog, string link) {
            //    Banshee.Web.Browser.Open(link);
            //});
        }
    }
}
