/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  Database.cs
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
using System.Data;
using System.Xml;
using System.Collections;
using Mono.Data.SqliteClient;

using Sql;

namespace Banshee
{
	public class Database 
	{
		private Hashtable threadConnections;
		private Hashtable tableColumns;
		private string dbname;
		private string dbpath;

        private bool writeInProgress;
        
        public event EventHandler WriteCycleFinished;

        public bool WriteInProgress {
            get {
                return writeInProgress;
           }
        }

		private IDbConnection dbcon
		{
			get {	
				return Connect();
			}
		}

		public Database(string dbname, string dbpath)
		{
			this.dbname = dbname;
			this.dbpath = dbpath;
			
			threadConnections = new Hashtable();
			tableColumns = new Hashtable();
			
			writeInProgress = false;
			
			Connect();
			InitializeTables();
		}
		
		// Returns a connection handle for the current thread... SQLite is 
		// not thread safe, and needs a new connection for each thread
		// a hashtable is used to store connection handles, where the key
		// is a handle to the executing thread
		private IDbConnection Connect()
		{
			if(threadConnections[System.Threading.Thread.CurrentThread] != null)
				return (IDbConnection)threadConnections[
					System.Threading.Thread.CurrentThread];
			
			IDbConnection conn = new SqliteConnection("Version=3,URI=file:" 
				+ dbpath);
			conn.Open();
			threadConnections[System.Threading.Thread.CurrentThread] = conn;
			return conn;
		}
		
		public void Close()
		{
			foreach(IDbConnection conn in threadConnections.Values) {
				conn.Close();
			}
		}

		public IDataReader Query(object query)
		{
			IDbCommand dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = query.ToString();
			return dbcmd.ExecuteReader();
		}
		
		public int Execute(object query)
		{
			IDbCommand dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = query.ToString();
			int ret = dbcmd.ExecuteNonQuery();
			
			EventHandler handler = WriteCycleFinished;
			if(handler != null)
			     handler(this, new EventArgs());
			
			return ret;
		}
		
		public object QuerySingle(object query)
		{
			IDbCommand dbcmd = dbcon.CreateCommand();
			dbcmd.CommandText = query.ToString();
			return dbcmd.ExecuteScalar();
		}
		
		// -- //
		
		private void InitializeTables()
		{		
			string data = Resource.GetFileContents("Tables.sql");
			ArrayList instructions = ParseRawSql(data);
			ExecuteSqlStatements(instructions);
			
			try {
			   Execute("PRAGMA synchronous = OFF");
			} catch(ApplicationException) {
			   DebugLog.Add("Could not set sqlite3 PRAGMA synchronous = OFF");
			}
			
			try {
			   QuerySingle("SELECT LastPlayedStamp FROM Tracks LIMIT 1");
			} catch(ApplicationException) {
			   Execute("ALTER TABLE Tracks ADD LastPlayedStamp INTEGER");
			}
			
			try {
			   QuerySingle("SELECT DateAddedStamp FROM Tracks LIMIT 1");
			} catch(ApplicationException) {
			   Execute("ALTER TABLE Tracks ADD DateAddedStamp INTEGER");
			}
		}
		
		private ArrayList ParseRawSql(string rawData)
		{
			ArrayList validInstructions = new ArrayList();
			
			string [] lines = rawData.Split('\n');
			string filteredData = null;

			foreach(string line in lines) {
				string tline = line.Trim();
				if(tline.StartsWith("--") && 
					!tline.StartsWith("--IF TABLE NOT EXISTS")) 
					continue;
				filteredData += tline + "\n";
			}
			
			string [] instructions = filteredData.Split(';');
			
			foreach(string ins in instructions) {
				string statement = ins.Trim();
				if(statement.Length <= 0)
					continue;
					
				validInstructions.Add(statement);	
			}
			
			return validInstructions;
		}
		
		private void ExecuteSqlStatements(ArrayList statements)
		{
			string db_use = null;
			
			for(int i = 0, n = statements.Count; i < n; i++) {
				string stmt = (string)statements[i];
				
				if(stmt.StartsWith("USE")) {
					string [] parts = stmt.Split(' ');
					if(parts.Length != 2)
						continue;
						
					db_use = parts[1].Trim();
					continue;
				} 
				
				if(!dbname.Equals(db_use))
					continue;
				
				if(stmt.StartsWith("--IF TABLE NOT EXISTS")) {
					string [] parts = stmt.Split(' ');
					if(parts.Length != 5)
						continue;

					if(TableExists(parts[4])) 
						i++;

					continue;
				}

				Execute(stmt);
			}
		}
		
		public bool TableExists(string table)
		{
			int count = Convert.ToInt32(QuerySingle(
				"SELECT COUNT(*) " +
				"FROM sqlite_master " + 
				"WHERE Type='table' AND Name='" + table + "'")
			);
			
			return count > 0;
		}
		
		// The following three methods parse the SQL used to create
		// a given table to provide information regarding the table
		
		private string [] GetRawTableColumnDefinitions(string table)
		{
			Statement query = new Select("sqlite_master", new List("sql")) +
				new Where(new Compare("name", Op.EqualTo, table));
				
			string tableDef = (string)QuerySingle(query);
			
			if(tableDef == null)
				return null;
				
			string s = tableDef.Substring(tableDef.IndexOf('(') + 1);
			s = s.Substring(0, s.IndexOf(')'));
			return s.Split(',');
		}
		
		public Hashtable ColumnMap(string table)
		{
			if(tableColumns[table] != null)
				return (Hashtable)tableColumns[table];
				
			Hashtable coltable = new Hashtable();
			
			string [] columnDefs = GetRawTableColumnDefinitions(table);
			
			for(int i = 0; i < columnDefs.Length; i++) {
				string [] parts = columnDefs[i].Split(' ');
				if(parts.Length <= 0)
					continue;
					
				string colname = parts[0].Trim();
				coltable.Add(i, colname);
				coltable.Add(colname, i);
			} 
			
			tableColumns[table] = coltable;
			
			return coltable;
		}
	}
}
