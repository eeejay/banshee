// Label.cs
//
// Copyright (c) 2008 Scott Peterson <lunchtimemama@gmail.com>
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
//

using System;
using System.Text;
using System.Xml;

namespace MusicBrainz
{
    public enum LabelType
    {
        Unspecified,
        Distributor,
        Holding,
        OriginalProduction,
        BootlegProduction,
        ReissueProduction
    }

    public sealed class Label : MusicBrainzEntity
    {
        const string EXTENSION = "label";
        
        protected override string UrlExtension {
            get { return EXTENSION; }
        }

        Label (string mbid) : base (mbid, null)
        {
        }

        Label (string mbid, string parameters) : base (mbid, parameters)
        {
        }

        internal Label (XmlReader reader) : base (reader, false)
        {
        }

        protected override void LoadMissingDataCore ()
        {
            Label label = new Label (Id, CreateInc ());
            type = label.Type;
            base.LoadMissingDataCore (label);
        }

        protected override bool ProcessAttributes (XmlReader reader)
        {
            type = Utils.StringToEnum<LabelType> (reader ["type"]);
            return this.type != null;
        }

        protected override bool ProcessXml (XmlReader reader)
        {
            reader.Read ();
            bool result = base.ProcessXml (reader);
            if (!result) {
                if (reader.Name == "country") {
                    result = true;
                    reader.Read ();
                    if (reader.NodeType == XmlNodeType.Text)
                        country = reader.ReadContentAsString ();
                } else reader.Skip (); // FIXME this is a workaround for Mono bug 334752
            }
            reader.Close ();
            return result;
        }

        string country;
        public string Country {
            get { return GetPropertyOrNull (ref country); }
        }

        LabelType? type;
        public LabelType Type {
            get { return GetPropertyOrDefault (ref type, LabelType.Unspecified); }
        }
        
        #region Static

        public static Label Get (string mbid)
        {
            if (mbid == null) throw new ArgumentNullException ("mbid");
            return new Label (mbid);
        }

        public static Query<Label> Query (string name)
        {
            if (name == null) throw new ArgumentNullException ("name");
            return new Query<Label> (EXTENSION, QueryLimit, CreateNameParameter (name));
        }

        public static Query<Label> QueryLucene (string luceneQuery)
        {
            if (luceneQuery == null) throw new ArgumentNullException ("luceneQuery");
            return new Query<Label> (EXTENSION, QueryLimit, CreateLuceneParameter (luceneQuery));
        }

        public static implicit operator string (Label label)
        {
            return label.ToString ();
        }
        
        #endregion

    }
}
