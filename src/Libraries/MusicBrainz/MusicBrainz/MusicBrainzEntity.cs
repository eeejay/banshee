/***************************************************************************
 *  MusicBrainzEntity.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;

namespace MusicBrainz
{
    // A person-like entity, such as an artist or a label.
    public abstract class MusicBrainzEntity : MusicBrainzObject
    {
        internal MusicBrainzEntity (string mbid, string parameters) : base (mbid, parameters)
        {
        }

        internal MusicBrainzEntity (XmlReader reader, bool all_rels_loaded) : base (reader, all_rels_loaded)
        {
        }

        protected override void HandleCreateInc (StringBuilder builder)
        {
            if (aliases == null) AppendIncParameters (builder, "aliases");
            base.HandleCreateInc (builder);
        }

        protected void HandleLoadMissingData (MusicBrainzEntity entity)
        {
            name = entity.Name;
            sort_name = entity.SortName;
            disambiguation = entity.Disambiguation;
            begin_date = entity.BeginDate;
            end_date = entity.EndDate;
            if (aliases == null) aliases = entity.Aliases;
            base.HandleLoadMissingData (entity);
        }

        protected override bool HandleXml (XmlReader reader)
        {
            bool result = true;
            switch (reader.Name) {
            case "name":
                reader.Read ();
                if (reader.NodeType == XmlNodeType.Text)
                    name = reader.ReadContentAsString ();
                break;
            case "sort-name":
                reader.Read ();
                if (reader.NodeType == XmlNodeType.Text)
                    sort_name = reader.ReadContentAsString ();
                break;
            case "disambiguation":
                reader.Read ();
                if (reader.NodeType == XmlNodeType.Text)
                    disambiguation = reader.ReadContentAsString ();
                break;
            case "life-span":
                begin_date = reader ["begin"];
                end_date = reader ["end"];
                break;
            case "alias-list":
                if (reader.ReadToDescendant ("alias")) {
                    List<string> aliases = new List<string> ();
                    do {
                        reader.Read ();
                        if (reader.NodeType == XmlNodeType.Text)
                            aliases.Add (reader.ReadContentAsString ());
                    } while (reader.ReadToNextSibling ("alias"));
                    this.aliases = aliases.AsReadOnly ();
                }
                break;
            default:
                result = false;
                break;
            }
            return result;
        }

        # region Properties

        string name;
        public virtual string Name {
            get { return GetPropertyOrNull (ref name); }
        }

        string sort_name;
        [Queryable]
        public virtual string SortName {
            get { return GetPropertyOrNull (ref sort_name); }
        }

        string disambiguation;
        [Queryable ("comment")]
        public virtual string Disambiguation {
            get { return GetPropertyOrNull (ref disambiguation); }
        }

        string begin_date;
        [Queryable ("begin")]
        public virtual string BeginDate {
            get { return GetPropertyOrNull (ref begin_date); }
        }

        string end_date;
        [Queryable ("end")]
        public virtual string EndDate {
            get { return GetPropertyOrNull (ref end_date); }
        }

        ReadOnlyCollection<string> aliases;
        [QueryableMember ("Contains", "alias")]
        public virtual ReadOnlyCollection<string> Aliases {
            get { return GetPropertyOrNew (ref aliases); }
        }

        #endregion

        protected static string CreateNameParameter (string name)
        {
            return "&name=" + Utils.PercentEncode (name);
        }

        public override string ToString ()
        {
            return name;
        }
    }
}
