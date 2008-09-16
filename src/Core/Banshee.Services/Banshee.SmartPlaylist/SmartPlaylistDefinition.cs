//
// SmartPlaylistDefinition.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

using Hyena.Query;
 
using Banshee.Query;
using Banshee.Sources;

namespace Banshee.SmartPlaylist
{
    public struct SmartPlaylistDefinition
    {
        public readonly string Name;
        public readonly string Description;
        public readonly string Condition;
        public readonly QueryOrder Order;
        public readonly QueryLimit Limit;
        public readonly IntegerQueryValue LimitNumber;

        public SmartPlaylistDefinition (string name, string description, string condition)
            : this (name, description, condition, 0, (QueryLimit)null, null)
        {
        }

        public SmartPlaylistDefinition (string name, string description, string condition,
                int limit_number, string limit, string order)
            : this (name, description, condition, limit_number, BansheeQuery.FindLimit (limit), BansheeQuery.FindOrder (order))
        {
        }

        public SmartPlaylistDefinition (string name, string description, string condition, 
            int limit_number, QueryLimit limit, QueryOrder order)
        {
            Name = name;
            Description = description;
            Condition = condition;
            LimitNumber = new IntegerQueryValue ();
            LimitNumber.SetValue (limit_number);
            Limit = limit;
            Order = order;
        }

        public SmartPlaylistSource ToSmartPlaylistSource (PrimarySource primary_source)
        {
            return new SmartPlaylistSource (
                Name,
                UserQueryParser.Parse (Condition, BansheeQuery.FieldSet),
                Order, Limit, LimitNumber,
                primary_source
            );
        }
    }
}
