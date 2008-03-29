/***************************************************************************
 *  Label.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

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

        protected override void HandleLoadMissingData ()
        {
            Label label = new Label (Id, CreateInc ());
            type = label.Type;
            base.HandleLoadMissingData (label);
        }

        protected override bool HandleAttributes (XmlReader reader)
        {
            type = Utils.StringToEnum<LabelType> (reader ["type"]);
            return this.type != null;
        }

        protected override bool HandleXml (XmlReader reader)
        {
            reader.Read ();
            bool result = base.HandleXml (reader);
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
