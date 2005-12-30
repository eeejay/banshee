/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  SqlGenerator.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections;
using System.Text.RegularExpressions; 

namespace Sql
{
	public class SqlGeneratorException : Exception
	{
		public SqlGeneratorException(string message) : base(message)
		{
		
		}
	}
	
	public class ColumnValueParser
	{
		public static void Parse(out ArrayList columns, 
			out ArrayList values, params object [] args)
		{
			columns = new ArrayList();
			values = new ArrayList();
		
			if(args.Length % 2 != 0) 
				throw new SqlGeneratorException("parameter count is not even");

			for(int i = 0, n = args.Length; i < n; i++) {
				if(args[i].GetType() != typeof(string))
					throw new SqlGeneratorException(
						"column name is not of type string");
				
				columns.Add(args[i++]);
				values.Add(args[i]);
			}
		}
	}
	
	public sealed class Op
	{
		public const string EqualTo = "=";
		public const string NullSafeEqualTo = "<=>";
		public const string NotEqualTo = "!=";
		public const string LessThan = "<";
		public const string GreaterThan = ">";
		public const string LessThanOrEqualTo = "<=";
		public const string GreaterThanOrEqualTo = ">=";
		
		public const string And = "AND";
		public const string Or = "OR";
	}
	
	public class Statement 
	{
		protected string statement;
		
		protected Statement()
		{
			statement = null;
		}
		
		public Statement(string statement)
		{
			this.statement = statement;
		}
		
		public override string ToString()
		{
			return statement;
		}
		
		public static Statement operator +(Statement stmt1, Statement stmt2)
		{
			return new Statement(stmt1.ToString() + " " + stmt2.ToString());
		}
		
		public static string EscapeQuotes(string str)
		{
		 	string s = Regex.Replace(str, "'", @"''");
		 	return s;
		}
		
		public static Statement Empty
		{
			get {
				return new Statement("");
			}
		}
	}
		
	public class Insert : Statement
	{
		public Insert(string table, bool keyPairs, params object [] args)
		{
			ArrayList columns = null;
			ArrayList values = null;
		
			statement = "INSERT INTO " + table + " ";
			
			if(keyPairs)
				ColumnValueParser.Parse(out columns, out values, args);
			else {
				values = new ArrayList();
				foreach(object o in args)
					values.Add(o);
			}
			
			if(keyPairs) {
				statement += "(";
				for(int i = 0, n = columns.Count; i < n; i++) {
					statement += (string)columns[i];
					if(i < n - 1)
						statement += ", ";
				}
				statement += ") ";
			}
			
			statement += "VALUES (";
			
			for(int i = 0, n = values.Count; i < n; i++) {
				if(values[i] == null)
					statement += "null";
				else
					statement += "'" + EscapeQuotes(values[i].ToString()) + "'";
				if(i < n - 1)
					statement += ", ";
			}
			
			statement += ")";
		}
		
		public static string Build(string table, bool keyPairs, 
			params object [] args)
		{
			Insert stmt = new Insert(table, keyPairs, args);
			return stmt.ToString();
		}
	} 
	
	public class Update : Statement
	{
		public Update(string table, params object [] args)
		{
			ArrayList columns = null;
			ArrayList values = null;
		
			statement = "UPDATE " + table + " SET ";
			ColumnValueParser.Parse(out columns, out values, args);
			
			for(int i = 0, n = columns.Count; i < n; i++) {
				statement += (string)columns[i] + " = " + 
					(values[i] == null ? "null" : 
						"'" + EscapeQuotes(values[i].ToString()) + "'");
				if(i < n - 1)
					statement += ", ";
			}
		}
		
		public static string Build(string table, params object [] args)
		{
			Update stmt = new Update(table, args);
			return stmt.ToString();
		}
	}
	
	public class Limit : Statement
	{
		public Limit(int count)
		{
			statement = "LIMIT " + count;
		}
		
		public Limit(int start, int count)
		{
			statement = "LIMIT " + start + ", " + count;
		}
	}
	
	public class Where : Statement
	{
		public Where(params object [] args)
		{
			if(args.Length % 2 == 0)
				throw new SqlGeneratorException("parameter count is not odd");

			statement = "WHERE ";

			foreach(object o in args)
				statement += o.ToString() + " ";
				
			statement = statement.Trim();
		}
		
		public Where()
		{
			statement = "WHERE ";
		}
	}
	
	public class Compare : Statement
	{
		public Compare(object left, object op, object right)
		{
			statement = left.ToString() + " " + op.ToString() + " '" + 
				EscapeQuotes(right.ToString()) + "'";
		}
	}
	
	public class Escape : Statement
	{
		public Escape(object o)
		{
			statement = EscapeQuotes(o.ToString());
		}
	}
	
	public class Quote : Statement
	{
		public Quote(object o)
		{
			statement = "'" + EscapeQuotes(o.ToString()) + "'";
		}
	}
	
	public class List : Statement
	{
		public List(params object [] args)
		{
			for(int i = 0, n = args.Length; i < n; i++) {
				statement += args[i];
				if(i < n - 1)
					statement += ", ";
			}	
		}
	}
	
	public class Select : Statement
	{
		public Select(string table)
		{
			statement = "SELECT * FROM " + table;
		}
	
		public Select(string table, Statement what)
		{
			statement = "SELECT " + what + " FROM " + table;
		}
	}
	
	public class Delete : Statement
	{
		public Delete(string table)
		{
			statement = "DELETE FROM " + table;
		}
	}
	
	public enum OrderDirection {
			Asc,
			Desc
	};
	
	public class OrderBy : Statement
	{
		public OrderBy(params object [] args)
		{
			statement = "ORDER BY";
		
			for(int i = 0, n = args.Length; i < n; i++) {
				Type type = args[i].GetType();
				
				if(type != typeof(string) && i == 0)
					throw new SqlGeneratorException(
						"First argument must be a column name");
			
				if(i > 0 && type == typeof(OrderDirection) && 
					args[i - 1].GetType() == typeof(OrderDirection))
					throw new SqlGeneratorException(
						"Order direction must be precede a column name");
						
				if(type == typeof(string))
					statement += " " + (args[i] as string);
				else if(type == typeof(OrderDirection)) {
					switch((OrderDirection)args[i]) {
						case OrderDirection.Asc:
							statement += " ASC";
							break;
						case OrderDirection.Desc:
							statement += " DESC";
							break;
					}
				} else {
					throw new SqlGeneratorException("Invalid type");
				}
				
				if(i < n - 1 && 
					args[i + 1].GetType() != typeof(OrderDirection)) {
					statement += ",";
				}
			}	
		}
	}
	
	public class Or : Statement
	{
		public Or()
		{
			statement = Op.Or;
		}
	}
	
	public class And : Statement
	{
		public And()
		{
			statement = Op.And;
		}
	}
	
	public class ParenGroup : Statement
	{
		public ParenGroup(Statement sub)
		{
			statement = " ( " + sub + " ) ";
		}
	}
}
