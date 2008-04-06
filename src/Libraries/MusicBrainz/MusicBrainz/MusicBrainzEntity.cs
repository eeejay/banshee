#region License

// MusicBrainzEntity.cs
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

#endregion

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
        
        #region Private
        
        string name;
        string sort_name;
        string disambiguation;
        string begin_date;
        string end_date;
        ReadOnlyCollection<string> aliases;
        
        #endregion
        
        #region Constructors
        
        internal MusicBrainzEntity (string mbid, string parameters) : base (mbid, parameters)
        {
        }

        internal MusicBrainzEntity (XmlReader reader, bool all_rels_loaded) : base (reader, all_rels_loaded)
        {
        }
        
        #endregion
        
        #region Protected

        protected override void CreateIncCore (StringBuilder builder)
        {
            if (aliases == null) AppendIncParameters (builder, "aliases");
            base.CreateIncCore (builder);
        }

        protected void LoadMissingDataCore (MusicBrainzEntity entity)
        {
            name = entity.Name;
            sort_name = entity.SortName;
            disambiguation = entity.Disambiguation;
            begin_date = entity.BeginDate;
            end_date = entity.EndDate;
            if (aliases == null) aliases = entity.Aliases;
            base.LoadMissingDataCore (entity);
        }

        protected override bool ProcessXml (XmlReader reader)
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
        
        protected static string CreateNameParameter (string name)
        {
            return "&name=" + Utils.PercentEncode (name);
        }
        
        #endregion

        #region Properties

        public virtual string Name {
            get { return GetPropertyOrNull (ref name); }
        }

        [Queryable]
        public virtual string SortName {
            get { return GetPropertyOrNull (ref sort_name); }
        }

        [Queryable ("comment")]
        public virtual string Disambiguation {
            get { return GetPropertyOrNull (ref disambiguation); }
        }

        [Queryable ("begin")]
        public virtual string BeginDate {
            get { return GetPropertyOrNull (ref begin_date); }
        }

        [Queryable ("end")]
        public virtual string EndDate {
            get { return GetPropertyOrNull (ref end_date); }
        }

        [QueryableMember ("Contains", "alias")]
        public virtual ReadOnlyCollection<string> Aliases {
            get { return GetPropertyOrNew (ref aliases); }
        }

        #endregion

        #region Public
        
        public override string ToString ()
        {
            return name;
        }
        
        #endregion
        
    }
}
