//
// QueryOrder.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//   Aaron Bockover <abockover@novell.com>
//
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
using System.Text;
using System.Collections.Generic;

namespace Hyena.Query
{
    public class QueryOrder
    {
        private string name;
        public string Name {
            get { return name; }
        }

        private string label;
        public string Label {
            get { return label; }
            set { label = value; }
        }

        private string order_sql;
        public string OrderSql {
            get { return order_sql; }
        }

        public QueryOrder (string name, string label, string order_sql)
        {
            this.name = name;
            this.label = label;
            this.order_sql = order_sql;
        }

        public string ToSql ()
        {
            return String.Format ("ORDER BY {0}", order_sql);
        }

        public string PruneSql (QueryLimit limit, IntegerQueryValue limit_value)
        {
            /*"SELECT {0}, {1} FROM {2}", "OrderID", limit.Column, "CoreCache";

            long limit = limit_value.IntValue *  limit.Factor;
            long sum = 0;
            int? limit_id = null;
            foreach (result) {
                sum += result[1];
                if (sum >= limit) {
                    limit_id = result[0];
                    break;
                }
            }

            if (limit_id != null) {
                "DELETE FROM {0} WHERE {1} > {2}", "CoreCache", "OrderID", limit_id
            }*/
            return null;
        }
    }
}
