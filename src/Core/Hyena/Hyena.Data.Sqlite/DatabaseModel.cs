//
// DatabaseModel.cs
//
// Author:
//   Scott Peterson  <lunchtimemama@gmail.com>
//
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
using System.Data;
using System.Reflection;
using System.Text;

namespace Hyena.Data.Sqlite
{
    public abstract class DatabaseModel<T>
    {
        private readonly List<DatabaseColumn> columns = new List<DatabaseColumn> ();
        private readonly List<VirtualDatabaseColumn> virtual_columns = new List<VirtualDatabaseColumn> ();
        
        private DatabaseColumn key;
        private HyenaSqliteConnection connection;
        
        private HyenaSqliteCommand create_command;
        private HyenaSqliteCommand insert_command;
        private HyenaSqliteCommand update_command;
        private HyenaSqliteCommand select_command;
        private HyenaSqliteCommand select_range_command;
        private HyenaSqliteCommand select_single_command;
        
        private string primary_key;
        private string select;
        private string from;
        private string where;
        
        private const string HYENA_DATABASE_NAME = "hyena_database_master";

        protected abstract string TableName { get; }
        protected abstract int ModelVersion { get; }
        protected abstract int DatabaseVersion { get; }
        protected abstract void MigrateTable (int old_version);
        protected abstract void MigrateDatabase (int old_version);
        //protected abstract T MakeNewObject (int offset);
        
        protected virtual string HyenaTableName {
            get { return "HyenaModelVersions"; }
        }
        
        protected HyenaSqliteConnection Connection {
            get { return connection; }
        }
        
        protected DatabaseModel (HyenaSqliteConnection connection)
        {
            this.connection = connection;
        }

        protected void Init ()
        {
            foreach (FieldInfo field in typeof(T).GetFields (BindingFlags.Instance | BindingFlags.NonPublic)) {
                foreach (Attribute attribute in field.GetCustomAttributes (true)) {
                    AddColumn (field, attribute);
                }
            }
            foreach (PropertyInfo property in typeof(T).GetProperties (BindingFlags.Instance | BindingFlags.Public)) {
                foreach (Attribute attribute in property.GetCustomAttributes (true)) {
                    AddColumn (property, attribute);
                }
            }
            foreach (PropertyInfo property in typeof(T).GetProperties (BindingFlags.Instance | BindingFlags.NonPublic)) {
                foreach (Attribute attribute in property.GetCustomAttributes (true)) {
                    AddColumn (property, attribute);
                }
            }
            
            if (key == null) {
                throw new Exception (String.Format ("The {0} table does not have a primary key", TableName));
            }
            
            CheckVersion ();
            CheckTable ();
        }
        
        private void CheckTable ()
        {
            Console.WriteLine ("In {0} checking for table {1}", this, TableName);
            using (IDataReader reader = connection.ExecuteReader (GetSchemaSql (TableName))) {
                if (reader.Read ()) {
                    Dictionary<string, string> schema = GetSchema (reader);
                    foreach (DatabaseColumn column in columns) {
                        if (!schema.ContainsKey (column.Name)) {
                            connection.Execute (String.Format (
                                "ALTER TABLE {0} ADD {1}", TableName, column.Schema));
                        }
                        if (column.Index != null) {
                            using (IDataReader index_reader = connection.ExecuteReader (GetSchemaSql (column.Index))) {
                                if (!index_reader.Read ()) {
                                    connection.Execute (String.Format (
                                        "CREATE INDEX {0} ON {1}({2})", column.Index, TableName, column.Name));
                                }
                            }
                        }
                    }
                } else {
                    CreateTable ();
                }
            }
        }
        
        private static Dictionary<string, string> GetSchema (IDataReader reader)
        {
            Dictionary<string, string> schema = new Dictionary<string, string> ();
            string sql = reader.GetString (0);
            sql = sql.Substring (sql.IndexOf ('(') + 1);
            foreach (string column_def in sql.Split (',')) {
                string column_def_t = column_def.Trim ();
                int ws_index = column_def_t.IndexOfAny (new char [] { ' ', '\t', '\n', '\r' });
                schema.Add (column_def_t.Substring (0, ws_index), null);
            }
            return schema;
        }
        
        protected virtual void CheckVersion ()
        {
            using (IDataReader reader = connection.ExecuteReader (GetSchemaSql (HyenaTableName))) {
                if (reader.Read ()) {
                    using (IDataReader table_reader = connection.ExecuteReader (String.Format (
                        "SELECT version FROM {0} WHERE name = '{1}'", HyenaTableName, TableName))) {
                        if (table_reader.Read ()) {
                            int version = table_reader.GetInt32 (0);
                            if (version < ModelVersion) {
                                MigrateTable (version);
                                connection.Execute (String.Format( 
                                    "UPDATE {0} SET version = {1} WHERE name = '{3}'",
                                    HyenaTableName, ModelVersion, TableName));
                            }
                        } else {
                            connection.Execute (String.Format (
                                "INSERT INTO {0} (name, version) VALUES ('{1}', {2})",
                                HyenaTableName, TableName, ModelVersion));
                        }
                    }
                    using (IDataReader db_reader = connection.ExecuteReader (String.Format (
                        "SELECT version FROM {0} WHERE name = '{1}'", HyenaTableName, HYENA_DATABASE_NAME))) {
                        db_reader.Read ();
                        int version = db_reader.GetInt32 (0);
                        if (version < DatabaseVersion) {
                            MigrateDatabase (version);
                            connection.Execute (String.Format (
                                "UPDATE {0} SET version = {1} WHERE name = '{2}'",
                                HyenaTableName, ModelVersion, HYENA_DATABASE_NAME));
                        }
                    }
                }
                else {
                    connection.Execute (String.Format (
                        "CREATE TABLE {0} (id INTEGER PRIMARY KEY, name TEXT UNIQUE, version INTEGER)", HyenaTableName));
                    connection.Execute (String.Format (
                        "INSERT INTO {0} (name, version) VALUES ('{1}', {2})",
                        HyenaTableName, HYENA_DATABASE_NAME, DatabaseVersion));
                    connection.Execute (String.Format (
                        "INSERT INTO {0} (name, version) VALUES ('{1}', {2})",
                        HyenaTableName, TableName, ModelVersion));
                }
            }
        }
        
        protected static string GetSchemaSql (string table_name)
        {
            return String.Format ("SELECT sql FROM sqlite_master WHERE name = '{0}'", table_name);
        }
        
        private void AddColumn (MemberInfo member, Attribute attribute)
        {
            DatabaseColumnAttribute column = attribute as DatabaseColumnAttribute;
            if (column != null) {
                DatabaseColumn c = member is FieldInfo
                    ? new DatabaseColumn ((FieldInfo)member, column)
                    : new DatabaseColumn ((PropertyInfo)member, column);
                
                foreach (DatabaseColumn col in columns) {
                    if (col.Name == c.Name) {
                        throw new Exception (String.Format ("{0} has multiple columns named {1}", TableName, c.Name));
                    }
                    if (col.Index != null && col.Index == c.Index) {
                        throw new Exception (String.Format ("{0} has multiple indecies named {1}", TableName, c.Name));
                    }
                }
                
                columns.Add (c);
                
                if ((c.Constraints & DatabaseColumnConstraints.PrimaryKey) > 0) {
                    if (key != null) {
                        throw new Exception (String.Format ("Multiple primary keys in the {0} table", TableName));
                    }
                    key = c;
                }
            }
            DatabaseVirtualColumnAttribute virtual_column = attribute as DatabaseVirtualColumnAttribute;
            if (virtual_column != null) {
                if (member is FieldInfo) {
                    virtual_columns.Add (new VirtualDatabaseColumn ((FieldInfo) member, virtual_column));
                } else {
                    virtual_columns.Add (new VirtualDatabaseColumn ((PropertyInfo) member, virtual_column));
                }
            }
        }
        
        protected virtual void CreateTable ()
        {
            connection.Execute (CreateCommand);
            foreach (DatabaseColumn column in columns) {
                if (column.Index != null) {
                    connection.Execute (String.Format (
                        "CREATE INDEX {0} ON {1}({2})", column.Index, TableName, column.Name));
                }
            }
        }
        
        protected virtual void PrepareInsertCommand (T target)
        {
            for (int i = 0; i < columns.Count; i++) {
                InsertCommand.Parameters[i].Value = columns[i].GetValue (target);
            }
        }
        
        public int Insert (T target)
        {
            PrepareInsertCommand (target);
            return connection.Execute (InsertCommand);
        }

        protected virtual void PrepareUpdateCommand (T target)
        {
            for (int i = 0; i < columns.Count; i++) {
                UpdateCommand.Parameters[i].Value = columns[i].GetValue (target);
            }
            UpdateCommand.Parameters[columns.Count].Value = key.GetValue (target);
        }
        
        public void Update (T target)
        {
            PrepareUpdateCommand (target);
            connection.Execute (UpdateCommand);
        }
        
        public void Load (T target, IDataReader reader)
        {
            int i = 0;
            
            foreach (DatabaseColumn column in columns) {
                column.SetValue (target, reader, i++);
            }
            
            foreach (VirtualDatabaseColumn column in virtual_columns) {
                column.SetValue (target, reader, i++);
            }
        }
        
        protected virtual void PrepareSelectCommand ()
        {
        }
        
        /*public IEnumerable<T> FetchAll ()
        {
            PrepareSelectCommand ();
            int i = 1;
            using (IDataReader reader = connection.ExecuteReader (SelectCommand)) {
                while (reader.Read ()) {
                    T new_object = MakeNewObject (i);
                    Load (new_object, reader);
                    yield return new_object;
                }
            }
        }*/
        
        protected virtual void PrepareSelectRangeCommand (int offset, int limit)
        {
            SelectRangeCommand.ApplyValues (offset, limit);
        }
        
        /*public IEnumerable<T> FetchRange (int offset, int limit)
        {
            PrepareSelectRangeCommand (offset, limit);
            using (IDataReader reader = connection.ExecuteReader (SelectRangeCommand)) {
                while (reader.Read ()) {
                    T new_object = MakeNewObject (offset++);
                    Load (new_object, reader);
                    yield return new_object;
                }
            }
        }*/
        
        protected virtual void PrepareSelectSingleCommand (object id)
        {
            SelectSingleCommand.ApplyValues (id);
        }
        
        /*public T FetchSingle (int id)
        {
            PrepareSelectSingleCommand (id);
            using (IDataReader reader = connection.ExecuteReader (SelectSingleCommand)) {
                if (reader.Read ()) {
                    T new_object = MakeNewObject (id);
                    Load (new_object, reader);
                    return new_object;
                }
            }
            return default(T);
        }*/
        
        protected virtual HyenaSqliteCommand CreateCommand {
            get {
                if (create_command == null) {
                    StringBuilder builder = new StringBuilder ();
                    builder.Append ("CREATE TABLE ");
                    builder.Append (TableName);
                    builder.Append ('(');
                    bool first = true;
                    foreach (DatabaseColumn column in columns) {
                        if (first) {
                            first = false;
                        } else {
                            builder.Append (',');
                        }
                        builder.Append (column.Schema);
                    }
                    builder.Append (')');
                    create_command = new HyenaSqliteCommand (builder.ToString ());
                }
                return create_command;
            }
        }
        
        protected virtual HyenaSqliteCommand InsertCommand {
            get {
                // FIXME can this string building be done more nicely?
                if (insert_command == null) {
                    StringBuilder cols = new StringBuilder ();
                    StringBuilder vals = new StringBuilder ();
                    int count = 0;
                    bool first = true;
                    foreach (DatabaseColumn column in columns) {
                        if (first) {
                            first = false;
                        } else {
                            cols.Append (',');
                            vals.Append (',');
                        }
                        cols.Append (column.Name);
                        vals.Append ('?');
                        count++;
                    }

                    insert_command = new HyenaSqliteCommand (String.Format (
                            "INSERT INTO {0} ({1}) VALUES ({2})",
                            TableName, cols.ToString (), vals.ToString ()), count);
                }
                return insert_command;
            }
        }
        
        protected virtual HyenaSqliteCommand UpdateCommand {
            get {
                if (update_command == null) {
                    StringBuilder builder = new StringBuilder ();
                    builder.Append ("UPDATE ");
                    builder.Append (TableName);
                    builder.Append (" SET ");
                    int count = 0;
                    bool first = true;
                    foreach (DatabaseColumn column in columns) {
                        if (first) {
                            first = false;
                        } else {
                            builder.Append (',');
                        }
                        builder.Append (column.Name);
                        builder.Append (" = ?");
                        count++;
                    }
                    builder.Append (" WHERE ");
                    builder.Append (key.Name);
                    builder.Append (" = ?");
                    count++;
                    update_command = new HyenaSqliteCommand (builder.ToString (), count);
                }
                return update_command;
            }
        }
        
        protected virtual HyenaSqliteCommand SelectCommand {
            get {
                if (select_command == null) {
                    select_command = new HyenaSqliteCommand (Where.Length > 0
                        ? String.Format ("SELECT {0} FROM {1} WHERE {2}", Select, From, Where)
                        : String.Format ("SELECT {0} FROM {1}", Select, From));
                }
                return select_command;
            }
        }
        
        protected virtual HyenaSqliteCommand SelectRangeCommand {
            get {
                if (select_range_command == null) {
                    select_range_command = new HyenaSqliteCommand (Where.Length > 0
                        ? String.Format ("SELECT {0} FROM {1} WHERE {2} LIMIT ?, ?", Select, From, Where)
                        : String.Format ("SELECT {0} FROM {1} LIMIT ?, ?", Select, From), 2);
                }
                return select_range_command;
            }
        }
        
        protected virtual HyenaSqliteCommand SelectSingleCommand {
            get {
                if (select_single_command == null) {
                    select_single_command = new HyenaSqliteCommand (Where.Length > 0
                        ? String.Format ("SELECT {0} FROM {1} WHERE {2} AND {3} = ?", Select, From, Where, PrimaryKey)
                        : String.Format ("SELECT {0} FROM {1} WHERE {2} = ?", Select, From, PrimaryKey), 1);
                }
                return select_single_command;
            }
        }
        
        public virtual string Select {
            get {
                if (select == null) {
                    BuildQuerySql ();
                }
                return select;
            }
        }
        
        public virtual string From {
            get {
                if (from == null) {
                    BuildQuerySql ();
                }
                return from;
            }
        }
        
        public virtual string Where {
            get {
                if (where == null) {
                    BuildQuerySql ();
                }
                return where;
            }
        }
        
        public string PrimaryKey {
            get {
                if (primary_key == null) {
                    primary_key = String.Format ("{0}.{1}", TableName, key.Name);
                }
                return primary_key;
            }
        }
        
        private void BuildQuerySql ()
        {
            StringBuilder select_builder = new StringBuilder ();
            bool first = true;
            foreach (DatabaseColumn column in columns) {
                if (first) {
                    first = false;
                } else {
                    select_builder.Append (',');
                }
                select_builder.Append (TableName);
                select_builder.Append ('.');
                select_builder.Append (column.Name);
            }
            
            StringBuilder where_builder = new StringBuilder ();
            Dictionary<string, string> tables = new Dictionary<string,string> (virtual_columns.Count + 1);
            tables.Add (TableName, null);
            bool first_virtual = true;
            foreach (VirtualDatabaseColumn column in virtual_columns) {
                if (first_virtual) {
                    first_virtual = false;
                } else {
                    where_builder.Append (" AND ");
                }
                if (first) {
                    first = false;
                } else {
                    select_builder.Append (',');
                }
                select_builder.Append (column.TargetTable);
                select_builder.Append ('.');
                select_builder.Append (column.Name);
                
                where_builder.Append (column.TargetTable);
                where_builder.Append ('.');
                where_builder.Append (column.ForeignKey);
                where_builder.Append (" = ");
                where_builder.Append (TableName);
                where_builder.Append ('.');
                where_builder.Append (column.LocalKey);
                
                if (!tables.ContainsKey (column.TargetTable)) {
                    tables.Add (column.TargetTable, null);
                }
            }
            
            StringBuilder from_builder = new StringBuilder ();
            bool first_tables = true;
            foreach (KeyValuePair<string, string> pair in tables) {
                if (first_tables) {
                    first_tables = false;
                } else {
                    from_builder.Append (',');
                }
                from_builder.Append (pair.Key);
            }

            select = select_builder.ToString ();
            from = from_builder.ToString ();
            where = where_builder.ToString ();
        }
	}
}
