/***************************************************************************
 *  Relation.cs
 *
 *  Authored by Scott Peterson <lunchtimemama@gmail.com>
 * 
 *  The author disclaims copyright to this source code.
 ****************************************************************************/

using System;

namespace MusicBrainz
{
    public enum RelationDirection
    {
        Forward,
        Backward
    }
    
    public abstract class RelationPrimative<T>
    {
        T target;
        string type;
        string[] attributes;
        RelationDirection direction;
        string begin;
        string end;

        internal RelationPrimative (string type, T target, RelationDirection direction,
            string begin, string end, string[] attributes)
        {
            this.type = type;
            this.target = target;
            this.direction = direction;
            this.begin = begin;
            this.end = end;
            this.attributes = attributes;
        }

        public T Target {
            get { return target; }
        }

        public string Type {
            get { return type; }
        }

        public string [] Attributes {
            get { return attributes; }
        }

        public RelationDirection Direction {
            get { return direction; }
        }

        public string BeginDate {
            get { return begin; }
        }
        
        public string EndDate {
            get { return end; }
        }
    }
    
    public sealed class Relation<T> : RelationPrimative<T> where T : MusicBrainzObject
    {
        internal Relation (string type,
                           T target,
                           RelationDirection direction,
                           string begin,
                           string end,
                           string [] attributes)
            : base (type, target, direction, begin, end, attributes)
        {
        }
    }

    public sealed class UrlRelation : RelationPrimative<string>
    {
        internal UrlRelation(string type,
                             string target,
                             RelationDirection direction,
                             string begin,
                             string end,
                             string [] attributes)
            : base (type, target, direction, begin, end, attributes)
        {
        }
    }
}
