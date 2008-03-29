/***************************************************************************
 *  Disc.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

using System;
using System.Xml;

namespace MusicBrainz
{
    public class Disc
    {
        string id;
        int sectors;

        internal Disc ()
        {
        }
        
        internal Disc (XmlReader reader)
        {
            reader.Read ();
            int.TryParse (reader ["sectors"], out sectors);
            id = reader ["id"];
            reader.Close ();
        }

        public string Id {
            get { return id; }
            protected set { id = value; }
        }

        public int Sectors {
            get { return sectors; }
            protected set { sectors = value; }
        }
    }
}
