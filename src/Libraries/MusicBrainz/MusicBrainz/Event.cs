/***************************************************************************
 *  Event.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

using System;
using System.Xml;

namespace MusicBrainz
{
    public sealed class Event
    {
        string date;
        string country;
        string catalog_number;
        string barcode;
        Label label;
        ReleaseFormat format = ReleaseFormat.None;

        internal Event (XmlReader reader)
        {
            reader.Read ();
            date = reader ["date"];
            country = reader ["country"];
            catalog_number = reader ["catalog-number"];
            barcode = reader ["barcode"];
            format = Utils.StringToEnum<ReleaseFormat> (reader ["format"]);
                if (reader.ReadToDescendant ("label")) {
                    label = new Label (reader.ReadSubtree ());
                    reader.Read (); // FIXME this is a workaround for Mono bug 334752
                }
            reader.Close ();
        }

        public string Date {
            get { return date; }
        }

        public string Country {
            get { return country; }
        }

        public string CatalogNumber {
            get { return catalog_number; }
        }

        public string Barcode {
            get { return barcode; }
        }

        public Label Label {
            get { return label; }
        }

        public ReleaseFormat Format {
            get { return format; }
        }
    }
}
