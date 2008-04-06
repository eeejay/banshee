// Relation.cs
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

namespace MusicBrainz
{
    public enum RelationDirection
    {
        Forward,
        Backward
    }
    
    public abstract class RelationBase<T>
    {
        T target;
        string type;
        string[] attributes;
        RelationDirection direction;
        string begin;
        string end;

        internal RelationBase (string type, T target, RelationDirection direction,
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
    
    public sealed class Relation<T> : RelationBase<T> where T : MusicBrainzObject
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

    public sealed class UrlRelation : RelationBase<string>
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
