//
// Migrator.cs
//
// Author:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2006-2007 Gabriel Burt
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Data;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

using Hyena.Data;
using Hyena.Query;
using Hyena.Data.Sqlite;

using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Query;

namespace Banshee.SmartPlaylist
{
    internal class Migrator
    {
        private string [] criteria = new string [] { "songs", "minutes", "hours", "MB" };
        private Dictionary<string, QueryOrder> order_hash = new Dictionary<string, QueryOrder> ();

        public static void MigrateAll ()
        {
            int version = ServiceManager.DbConnection.QueryInt32 ("SELECT Value FROM CoreConfiguration WHERE Key = 'SmartPlaylistVersion'");
            if (version == 1)
                return;

            Migrator m = new Migrator ();
            using (IDataReader reader = ServiceManager.DbConnection.ExecuteReader (
                "SELECT SmartPlaylistID, Name, Condition, OrderBy, LimitNumber, LimitCriterion FROM CoreSmartPlaylists")) {
                while (reader.Read ()) {
                    m.Migrate (
                        Convert.ToInt32 (reader[0]), reader[1] as string,
                        reader[2] as string, reader[3] as string,
                        reader[4] as string, reader[5] as string
                    );
                }
            }

            ServiceManager.DbConnection.Execute ("INSERT INTO CoreConfiguration (Key, Value) Values ('SmartPlaylistVersion', 1)");
        }

        public Migrator ()
        {
            order_hash.Add ("RANDOM()",             BansheeQuery.FindOrder ("Random", true));
            order_hash.Add ("AlbumTitle",           BansheeQuery.FindOrder ("Album", true));
            order_hash.Add ("Artist",               BansheeQuery.FindOrder ("Artist", true));
            order_hash.Add ("Genre",                BansheeQuery.FindOrder ("Genre", true));
            order_hash.Add ("Title",                BansheeQuery.FindOrder ("Title", true));
            order_hash.Add ("Rating DESC",          BansheeQuery.FindOrder ("Rating", false));
            order_hash.Add ("Rating ASC",           BansheeQuery.FindOrder ("Rating", true));
            order_hash.Add ("NumberOfPlays DESC",   BansheeQuery.FindOrder ("PlayCount", false));
            order_hash.Add ("NumberOfPlays ASC",    BansheeQuery.FindOrder ("PlayCount", true));
            order_hash.Add ("DateAddedStamp DESC",  BansheeQuery.FindOrder ("DateAddedStamp", false));
            order_hash.Add ("DateAddedStamp ASC",   BansheeQuery.FindOrder ("DateAddedStamp", true));
            order_hash.Add ("LastPlayedStamp DESC", BansheeQuery.FindOrder ("LastPlayedStamp", false));
            order_hash.Add ("LastPlayedStamp ASC",  BansheeQuery.FindOrder ("LastPlayedStamp", true));
        }

        private void Migrate (int dbid, string Name, string Condition, string OrderBy, string LimitNumber, string LimitCriterion)
        {
            Console.WriteLine ("migrating {0}, cond = {1}, order = {2}", Name, Condition, OrderBy);
            if (OrderBy != null && OrderBy != String.Empty) {
                QueryOrder order = order_hash [OrderBy];
                OrderBy = order.Name;
            }

            LimitCriterion = criteria [Convert.ToInt32 (LimitCriterion)];
            string ConditionXml = ParseCondition (Condition);

            ServiceManager.DbConnection.Execute (new HyenaSqliteCommand (@"
                UPDATE CoreSmartPlaylists
                    SET Name = ?,
                        Condition = ?,
                        OrderBy = ?,
                        LimitNumber = ?,
                        LimitCriterion = ?
                    WHERE SmartPlaylistID = ?",
                Name, ConditionXml, OrderBy, LimitNumber, LimitCriterion, dbid
            ));
            Console.WriteLine ("migrated {0}, cond = {1}, order = {2}", Name, ConditionXml, OrderBy);
        }

        private string ParseCondition (string value)
        {
            // Check for ANDs or ORs and split into conditions as needed
            string [] conditions;
            bool ands = true;
            if (value.IndexOf(") AND (") != -1) {
                ands = true;
                conditions = System.Text.RegularExpressions.Regex.Split (value, "\\) AND \\(");
            } else if (value.IndexOf(") OR (") != -1) {
                ands = false;
                conditions = System.Text.RegularExpressions.Regex.Split (value, "\\) OR \\(");
            } else {
                conditions = new string [] {value};
            }

            QueryListNode root = new QueryListNode (ands ? Keyword.And : Keyword.Or);

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
                foreach (QueryOperator op in QueryOperator.Operators) {
                    if (op.MatchesCondition (condition, out col, out v1, out v2)) {
                        QueryTermNode term = new QueryTermNode ();
                        QueryField field = BansheeQuery.FieldSet [col];
                        bool is_relative_date = false;
                        if (field == null) {
                            if (col.IndexOf ("DateAddedStamp") != -1) {
                                field = BansheeQuery.FieldSet ["added"];
                            } else if (col.IndexOf ("LastPlayedStamp") != -1) {
                                field = BansheeQuery.FieldSet ["lastplayed"];
                            }

                            // Fix ugly implementation of playlist/smart playlist conditions
                            if (op == QueryOperator.InPlaylist || op == QueryOperator.NotInPlaylist) {
                                field = BansheeQuery.FieldSet ["playlist"];
                            } else if (op == QueryOperator.InSmartPlaylist || op == QueryOperator.NotInSmartPlaylist) {
                                field = BansheeQuery.FieldSet ["smartplaylist"];
                            }

                            if (field == null) {
                                continue;
                            }
                            is_relative_date = true;
                        }

                        term.Field = field;

                        if (op == QueryOperator.Between) {
                            QueryListNode and = new QueryListNode (Keyword.And);
                            QueryTermNode t2 = new QueryTermNode ();
                            t2.Field = term.Field;

                            if (is_relative_date) {
                                ParseRelativeDateCondition (term, v1, field, ">=");
                                ParseRelativeDateCondition (t2, v2, field, "<=");
                            } else {
                                term.Value = QueryValue.CreateFromUserQuery (v1, field);
                                term.Operator = term.Value.OperatorSet ["<="];

                                t2.Value = QueryValue.CreateFromUserQuery (v2, field);
                                t2.Operator = t2.Value.OperatorSet [">="];
                            }
                            and.AddChild (term);
                            and.AddChild (t2);
                            root.AddChild (and);
                        } else if (is_relative_date) {
                            ParseRelativeDateCondition (term, v1, field, op.NewOp);
                            root.AddChild (term);
                        } else {
                            term.Value = QueryValue.CreateFromUserQuery (v1, field);
                            term.Operator = term.Value.OperatorSet [op.NewOp];
                            root.AddChild (term);
                        }

                        break;
                    }
                }

                count++;
            }

            QueryNode node = root.Trim ();

            if (node != null) {
                //Console.WriteLine ("After XML: {0}", node.ToXml (BansheeQuery.FieldSet, true));
                //Console.WriteLine ("After SQL: {0}", node.ToSql (BansheeQuery.FieldSet));
            }

            return node == null ? String.Empty : node.ToXml (BansheeQuery.FieldSet);
        }

        private void ParseRelativeDateCondition (QueryTermNode term, string val, QueryField field, string op)
        {
            string new_op = op.Replace ('>', '^');
            new_op = new_op.Replace ('<', '>');
            new_op = new_op.Replace ('^', '<');

            DateQueryValue date_value = new DateQueryValue ();

            // Have to flip the operator b/c of how we used to construct the SQL query
            term.Operator = date_value.OperatorSet [new_op];

            // Have to negate the value b/c of how we used to constuct the SQL query
            date_value.SetRelativeValue (-Convert.ToInt64 (val), RelativeDateFactor.Second);
            term.Value = date_value;
        }

        public sealed class QueryOperator
        {
            public string NewOp;
            private string format;

            public string Format {
                get { return format; }
            }

            private QueryOperator (string new_op, string format)
            {
                NewOp = new_op;
                this.format = format;
            }

            public string FormatValues (bool text, string column, string value1, string value2)
            {
                if (text)
                    return String.Format (format, "'", column, value1, value2);
                else
                    return String.Format (format, "", column, value1, value2);
            }

            public bool MatchesCondition (string condition, out string column, out string value1, out string value2) {
                // Remove trailing parens from the end of the format b/c trailing parens are trimmed from the condition
                string regex = String.Format(format.Replace("(", "\\(").Replace(")", "\\)"),
                        "'?",   // ignore the single quotes if they exist
                        "(.*)", // match the column
                        "(.*)", // match the first value
                        "(.*)"  // match the second value
                );


                //Console.WriteLine ("regex = {0}", regex);
                MatchCollection mc = System.Text.RegularExpressions.Regex.Matches (condition, regex);
                if (mc != null && mc.Count > 0 && mc[0].Groups.Count > 0) {
                    column = mc[0].Groups[1].Captures[0].Value;
                    value1 = mc[0].Groups[2].Captures[0].Value.Trim(new char[] {'\''});

                    if (mc[0].Groups.Count == 4)
                        value2 = mc[0].Groups[3].Captures[0].Value.Trim(new char[] {'\''});
                    else
                        value2 = null;

                    return true;
                } else {
                    column = value1 = value2 = null;
                    return false;
                }
            }

            // calling lower() to have case insensitive comparisons with strings
            public static QueryOperator EQText     = new QueryOperator("==", "lower({1}) = {0}{2}{0}");
            public static QueryOperator NotEQText  = new QueryOperator("!=", "lower({1}) != {0}{2}{0}");

            public static QueryOperator EQ         = new QueryOperator("==", "{1} = {0}{2}{0}");
            public static QueryOperator NotEQ      = new QueryOperator("!=", "{1} != {0}{2}{0}");
            // TODO how to deal w/ between?
            public static QueryOperator Between    = new QueryOperator("", "{1} BETWEEN {0}{2}{0} AND {0}{3}{0}");
            public static QueryOperator LT         = new QueryOperator("<", "{1} < {0}{2}{0}");
            public static QueryOperator GT         = new QueryOperator(">", "{1} > {0}{2}{0}");
            public static QueryOperator GTE        = new QueryOperator(">=", "{1} >= {0}{2}{0}");

            // Note, the following lower() calls are necessary b/c of a sqlite bug which makes the LIKE
            // command case sensitive with certain characters.
            public static QueryOperator Like       = new QueryOperator(":", "lower({1}) LIKE '%{2}%'");
            public static QueryOperator NotLike    = new QueryOperator("!:", "lower({1}) NOT LIKE '%{2}%'");
            public static QueryOperator StartsWith = new QueryOperator("=", "lower({1}) LIKE '{2}%'");
            public static QueryOperator EndsWith   = new QueryOperator(":=", "lower({1}) LIKE '%{2}'");

            // TODO these should either be made generic or moved somewhere else since they are Banshee/Track/Playlist specific.
            public static QueryOperator InPlaylist      = new QueryOperator("==", "TrackID IN (SELECT TrackID FROM PlaylistEntries WHERE {1} = {0}{2}{0})");
            public static QueryOperator NotInPlaylist   = new QueryOperator("!=", "TrackID NOT IN (SELECT TrackID FROM PlaylistEntries WHERE {1} = {0}{2}{0})");

            public static QueryOperator InSmartPlaylist      = new QueryOperator("==", "TrackID IN (SELECT TrackID FROM SmartPlaylistEntries WHERE {1} = {0}{2}{0})");
            public static QueryOperator NotInSmartPlaylist   = new QueryOperator("!=", "TrackID NOT IN (SELECT TrackID FROM SmartPlaylistEntries WHERE {1} = {0}{2}{0})");

            public static QueryOperator [] Operators = new QueryOperator [] {
                EQText, NotEQText, EQ, NotEQ, Between, LT, GT, GTE, Like, NotLike,
                StartsWith, InPlaylist, NotInPlaylist, InSmartPlaylist, NotInSmartPlaylist
            };
        }

        /*public sealed class QueryFilter
        {
            private string name;
            private QueryOperator op;

            public string Name {
                get { return name; }
            }

            public QueryOperator Operator {
                get { return op; }
            }

            private static Hashtable filters = new Hashtable();
            private static ArrayList filters_array = new ArrayList();
            public static QueryFilter GetByName (string name)
            {
                return filters[name] as QueryFilter;
            }

            public static ArrayList Filters {
                get { return filters_array; }
            }

            private static QueryFilter NewOperation (string name, QueryOperator op)
            {
                QueryFilter filter = new QueryFilter(name, op);
                filters[name] = filter;
                filters_array.Add (filter);
                return filter;
            }

            private QueryFilter (string name, QueryOperator op)
            {
                this.name = name;
                this.op = op;
            }

            public static QueryFilter InPlaylist = NewOperation (
                Catalog.GetString ("is"),
                QueryOperator.InPlaylist
            );

            public static QueryFilter NotInPlaylist = NewOperation (
                Catalog.GetString ("is not"),
                QueryOperator.NotInPlaylist
            );

            public static QueryFilter InSmartPlaylist = NewOperation (
                Catalog.GetString ("is"),
                QueryOperator.InSmartPlaylist
            );

            public static QueryFilter NotInSmartPlaylist = NewOperation (
                Catalog.GetString ("is not"),
                QueryOperator.NotInSmartPlaylist
            );
        
            // caution: the equal/not-equal operators for text fields (TextIs and TextNotIs) have to be defined
            // before the ones for non-text fields. Otherwise MatchesCondition will not return the right column names.
            // (because the regular expression for non-string fields machtes also for string fields)
            public static QueryFilter TextIs = NewOperation (
                Catalog.GetString ("is"),
                QueryOperator.EQText
            );

            public static QueryFilter TextIsNot = NewOperation (
                Catalog.GetString ("is not"),
                QueryOperator.NotEQText
            );

            public static QueryFilter Is = NewOperation (
                Catalog.GetString ("is"),
                QueryOperator.EQ
            );

            public static QueryFilter IsNot = NewOperation (
                Catalog.GetString ("is not"),
                QueryOperator.NotEQ
            );

            public static QueryFilter IsLessThan = NewOperation (
                Catalog.GetString ("is less than"),
                QueryOperator.LT
            );

            public static QueryFilter IsGreaterThan = NewOperation (
                Catalog.GetString ("is greater than"),
                QueryOperator.GT
            );

            public static QueryFilter MoreThan = NewOperation (
                Catalog.GetString ("more than"),
                QueryOperator.GT
            );

            public static QueryFilter LessThan = NewOperation (
                Catalog.GetString ("less than"),
                QueryOperator.LT
            );

            public static QueryFilter IsAtLeast = NewOperation (
                Catalog.GetString ("is at least"),
                QueryOperator.GTE
            );

            public static QueryFilter Contains = NewOperation (
                Catalog.GetString ("contains"),
                QueryOperator.Like
            );

            public static QueryFilter DoesNotContain = NewOperation (
                Catalog.GetString ("does not contain"),
                QueryOperator.NotLike
            );

            public static QueryFilter StartsWith = NewOperation (
                Catalog.GetString ("starts with"),
                QueryOperator.StartsWith
            );

            public static QueryFilter EndsWith = NewOperation (
                Catalog.GetString ("ends with"),
                QueryOperator.EndsWith
            );

            public static QueryFilter IsBefore = NewOperation (
                Catalog.GetString ("is before"),
                QueryOperator.LT
            );

            public static QueryFilter IsAfter = NewOperation (
                Catalog.GetString ("is after"),
                QueryOperator.GT
            );

            public static QueryFilter IsInTheRange = NewOperation (
                Catalog.GetString ("is between"),
                QueryOperator.Between
            );

            public static QueryFilter Between = NewOperation (
                Catalog.GetString ("between"),
                QueryOperator.Between
            );
        }*/
    }
}
