//
// Migrator.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2006-2007 Gabriel Burt
// Copyright (C) 2007 Novell, Inc.
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

namespace Banshee.SmartPlaylist
{
    public class Migrator
    {
        private string [] criteria = new string [] { "tracks", "minutes", "hours", "MB" };
        private Dictionary<string, Order> order_hash = new Dictionary<string, Order> ();

        public static void MigrateAll ()
        {
            Console.WriteLine ("Migrating All..........");
            Migrator m = new Migrator ();
            foreach (SmartPlaylistSource source in SmartPlaylistSource.LoadAll ())
                m.Migrate (source);
        }

        public Migrator ()
        {
            order_hash.Add ("RANDOM()", FindOrder ("Random"));
            order_hash.Add ("AlbumTitle", FindOrder ("Album"));
            order_hash.Add ("Artist", FindOrder ("Artist"));
            order_hash.Add ("Genre", FindOrder ("Genre"));
            order_hash.Add ("Title", FindOrder ("Title"));
            order_hash.Add ("Rating DESC", FindOrder ("Rating", "DESC"));
            order_hash.Add ("Rating ASC", FindOrder ("Rating", "ASC"));
            order_hash.Add ("NumerOfPlays DESC", FindOrder ("PlayCount", "DESC"));
            order_hash.Add ("NumerOfPlays ASC", FindOrder ("PlayCount", "ASC"));
            order_hash.Add ("DateAddedStamp DESC", FindOrder ("DateAddedStamp", "DESC"));
            order_hash.Add ("DateAddedStamp ASC", FindOrder ("DateAddedStamp", "ASC"));
            order_hash.Add ("LastPlayedStamp DESC", FindOrder ("LastPlayedStamp", "DESC"));
            order_hash.Add ("LastPlayedStamp ASC", FindOrder ("LastPlayedStamp", "ASC"));
        }

        private void Migrate (SmartPlaylistSource source)
        {
            Console.WriteLine ("migrating {0}, order {1}", source.Name, source.OrderBy);
            if (source.OrderBy != null && source.OrderBy != String.Empty) {
                Order order = order_hash [source.OrderBy];
                source.OrderBy = order.Key;
                source.OrderDir = order.Dir;
            }
            source.LimitCriterion = criteria [Convert.ToInt32 (source.LimitCriterion)];
            source.Condition = ParseCondition (source.Condition);
            source.Save ();
        }

        private string ParseCondition (string value)
        {
            /*
            // Check for ANDs or ORs and split into conditions as needed
            string [] conditions;
            bool ands;
            if (value.IndexOf(") AND (") != -1) {
                ands = true;
                conditions = System.Text.RegularExpressions.Regex.Split (value, "\\) AND \\(");
                
            } else if (value.IndexOf(") OR (") != -1) {
                ands = false;
                conditions = System.Text.RegularExpressions.Regex.Split (value, "\\) OR \\(");
            } else {
                conditions = new string [] {value};
            }

            // Remove leading spaces and parens from the first condition
            conditions[0] = conditions[0].Remove(0, 2);

            // Remove trailing spaces and last paren from the last condition
            string tmp = conditions[conditions.Length-1];
            tmp = tmp.TrimEnd(new char[] {' '});
            tmp = tmp.Substring(0, tmp.Length - 1);
            conditions[conditions.Length-1] = tmp;

            int count = 0;
            foreach (string condition in conditions) {
                // Add a new row for this condition
                string col, v1, v2;
                bool found_filter = false;
                foreach (QueryFilter filter in QueryFilter.Filters) {
                    if (filter.Operator.MatchesCondition (condition, out col, out v1, out v2)) {
                        //Console.WriteLine ("{0} is col: {1} with v1: {2} v2: {3}", condition, col, v1, v2);

                        // Set the column
                        QueryBuilderMatchRow row = matchesBox.Children[count] as QueryBuilderMatchRow;
                        if (!ComboBoxUtil.SetActiveString (row.FieldBox, model.GetName(col))) {
                            if (col.IndexOf ("current_timestamp") == -1) {
                                Console.WriteLine ("Found col that can't place");
                                break;
                            } else {
                                bool found = false;
                                foreach (string field in model) {
                                    if (col.IndexOf (model.GetColumn (field)) != -1) {
                                        ComboBoxUtil.SetActiveString (row.FieldBox, field);
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found) {
                                    Console.WriteLine ("Found col that can't place");
                                    break;
                                }
                            }
                        }

                        // Make sure we're on the right filter (as multiple filters can have the same operator)
                        QueryFilter real_filter = filter;
                        if (System.Array.IndexOf (row.Match.ValidFilters, filter) == -1) {
                            foreach (QueryFilter f in row.Match.ValidFilters) {
                                if (f.Operator == filter.Operator) {
                                    real_filter = f;
                                    break;
                                }
                            }
                        }

                        // Set the operator
                        if (!ComboBoxUtil.SetActiveString (row.FilterBox, real_filter.Name)) {
                            Console.WriteLine ("Found filter that can't place");
                            break;
                        }

                        // Set the values
                        row.Match.Value1 = v1;
                        row.Match.Value2 = v2;

                        found_filter = true;
                        break;
                    }
                }

                // TODO should push error here instead
                if (!found_filter)
                    Console.WriteLine ("Couldn't find appropriate filter for condition: {0}", condition);
                count++;
            }
        */
            return "CoreTracks.Rating > 0";
        }

        private Order FindOrder (string key)
        {
            return FindOrder (key, null);
        }

        private Order FindOrder (string key, string dir)
        {
            foreach (Order o in SmartPlaylistSource.Orders) {
                if (o.Key == key && (dir == null || o.Dir == dir))
                    return o;
            }
            return default(Order);
        }
    }
}
