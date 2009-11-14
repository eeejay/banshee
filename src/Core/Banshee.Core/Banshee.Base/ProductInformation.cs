//
// ProductInformation.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
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
using System.Collections.Generic;
using System.Xml;

namespace Banshee.Base
{
    public static class ProductInformation
    {
        private static SortedList<string, ProductAuthor> authors = new SortedList<string, ProductAuthor>  ();
        private static SortedList<string, ProductTranslation> translations
            = new SortedList<string, ProductTranslation> ();
        private static string [] artists;
        private static string [] contributors;

        static ProductInformation ()
        {
        //    try {
                LoadContributors ();
                LoadTranslators ();
       //     } catch {
     //       }
        }

        private static void LoadContributors ()
        {
            List<string> artists_list = new List<string> ();
            List<string> contributors_list = new List<string> ();

            XmlDocument doc = new XmlDocument ();
            doc.LoadXml (AssemblyResource.GetFileContents ("contributors.xml"));

            foreach (XmlNode node in doc.DocumentElement.ChildNodes) {
                if (node.FirstChild == null || node.FirstChild.Value == null) {
                    continue;
                }

                string name = node.FirstChild.Value.Trim ();

                switch (node.Name) {
                    case "author":
                        authors.Add (name, new ProductAuthor (name, node.Attributes["role"].Value));
                        break;
                    case "contributor":
                        contributors_list.Add (name);
                        break;
                    case "artist":
                        artists_list.Add (name);
                        break;
                    default:
                        break;
                }
            }

            artists = artists_list.ToArray ();
            contributors = contributors_list.ToArray ();

            Array.Sort (artists);
            Array.Sort (contributors);
        }

        private static void LoadTranslators ()
        {
            XmlDocument doc = new XmlDocument ();
            doc.LoadXml (AssemblyResource.GetFileContents ("translators.xml"));

            foreach (XmlNode node in doc.DocumentElement.ChildNodes) {
                if (node.Name != "language") {
                    continue;
                }

                try {
                    string language_code = node.Attributes["code"].Value.Trim ();
                    string language_name = node.Attributes["name"].Value.Trim ();

                    ProductTranslation translation = new ProductTranslation (language_code, language_name);

                    foreach (XmlNode person in node.ChildNodes) {
                        if (person.Name != "person") {
                            continue;
                        }

                        translation.AddTranslator (person.FirstChild.Value.Trim ());
                    }

                    translations.Add (language_name, translation);
                } catch {
                }
            }
        }

        public static IEnumerable<ProductTranslation> Translations {
            get { return translations.Values; }
        }

        public static IEnumerable<ProductAuthor> Authors {
            get { return authors.Values; }
        }

        public static string [] Contributors {
            get { return contributors; }
        }

        public static string [] Artists {
            get { return artists; }
        }

        public static string License {
            get { return AssemblyResource.GetFileContents ("COPYING"); }
        }
    }

    public class ProductTranslation
    {
        private string language_code;
        private string language_name;
        private SortedList<string, string> translators = new SortedList<string, string> ();

        private ProductTranslation ()
        {
        }

        internal ProductTranslation (string languageCode, string languageName)
        {
            language_code = languageCode;
            language_name = languageName;
        }

        internal void AddTranslator (string translator)
        {
            translators.Add (translator, translator);
        }

        public string LanguageCode {
            get { return language_code; }
        }

        public string LanguageName {
            get { return language_name; }
        }

        public IEnumerable<string> Translators {
            get { return translators.Values; }
        }
    }

    public class ProductAuthor
    {
        private string name;
        private string role;

        private ProductAuthor ()
        {
        }

        internal ProductAuthor (string name, string role)
        {
            if (name == null || role == null) {
                throw new ArgumentNullException ("name or role cannot be null");
            }

            this.name = name;
            this.role = role;
        }

        public string Name {
            get { return name; }
        }

        public string Role {
            get { return role; }
        }
    }
}
