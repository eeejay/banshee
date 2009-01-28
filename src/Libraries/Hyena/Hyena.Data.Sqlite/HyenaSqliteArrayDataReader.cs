//
// HyenaSqliteArrayDataReader.cs
//
// Authors:
//   Vladimir Vukicevic  <vladimir@pobox.com>
//   Everaldo Canuto  <everaldo_canuto@yahoo.com.br>
//   Joshua Tauberer <tauberer@for.net>
//   John Millikin <jmillikin@gmail.com>
//
// Copyright (C) 2002  Vladimir Vukicevic
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
using System.Data.Common;
using System.Text;
using System.Collections;
using Mono.Data.Sqlite;

namespace Hyena.Data.Sqlite
{
    /** Adapted from Mono.Data.SqliteClient.SqliteDataReader
     *
     * The new data reader in Mono.Data.Sqlite lazily loads the resultset
     * from the underlying database cursor. This class reads the entire
     * resultset into memory, allowing further queries to be executed before
     * all data readers have been exhausted.
    **/
    internal class HyenaSqliteArrayDataReader : MarshalByRefObject, IEnumerable, IDataReader, IDisposable, IDataRecord
    {
        #region Fields

        private ArrayList rows;
        private string[] columns;
        private Hashtable column_names_sens, column_names_insens;
        private int current_row;
        private bool closed;
        private int records_affected;
        private string[] decltypes;

        #endregion

        #region Constructors and destructors

        internal HyenaSqliteArrayDataReader (SqliteDataReader reader)
        {
            rows = new ArrayList ();
            column_names_sens = new Hashtable ();
            column_names_insens = new Hashtable (StringComparer.InvariantCultureIgnoreCase);
            closed = false;
            current_row = -1;
            ReadAllRows (reader);
        }

        #endregion

        #region Properties

        public int Depth {
            get { return 0; }
        }

        public int FieldCount {
            get { return columns.Length; }
        }

        public object this[string name] {
            get {
                return GetValue (GetOrdinal (name));
            }
        }

        public object this[int i] {
            get { return GetValue (i); }
        }

        public bool IsClosed {
            get { return closed; }
        }

        public int RecordsAffected {
            get { return records_affected; }
        }

        #endregion

        #region Internal Methods

        internal void ReadAllRows (SqliteDataReader reader)
        {
            int ii, field_count = reader.FieldCount;

            /* Metadata */
            records_affected = reader.RecordsAffected;

            decltypes = new string[field_count];
            for (ii = 0; ii < field_count; ii++)
                    decltypes[ii] = reader.GetDataTypeName (ii);

            columns = new string[field_count];
            for (ii = 0; ii < field_count; ii++)
            {
                    string column_name = reader.GetName (ii);
                    columns[ii] = column_name;
                    column_names_sens[column_name] = ii;
                    column_names_insens[column_name] = ii;
            }

            /* Read all rows, store in this->rows */
            while (reader.Read ())
            {
                object[] data_row = new object[field_count];
                for (ii = 0; ii < field_count; ii++)
                {
                        object value = reader.GetValue (ii);
                        if (Convert.IsDBNull (value))
                            value = null;
                        data_row[ii] = value;
                }
                rows.Add (data_row);
            }
        }

        #endregion

        #region  Public Methods

        public void Close ()
        {
            closed = true;
        }

        public void Dispose ()
        {
            Close ();
        }

        IEnumerator IEnumerable.GetEnumerator ()
        {
            return new DbEnumerator (this);
        }

        public DataTable GetSchemaTable ()
        {
            DataTable dataTableSchema = new DataTable ();

            dataTableSchema.Columns.Add ("ColumnName", typeof (String));
            dataTableSchema.Columns.Add ("ColumnOrdinal", typeof (Int32));
            dataTableSchema.Columns.Add ("ColumnSize", typeof (Int32));
            dataTableSchema.Columns.Add ("NumericPrecision", typeof (Int32));
            dataTableSchema.Columns.Add ("NumericScale", typeof (Int32));
            dataTableSchema.Columns.Add ("IsUnique", typeof (Boolean));
            dataTableSchema.Columns.Add ("IsKey", typeof (Boolean));
            dataTableSchema.Columns.Add ("BaseCatalogName", typeof (String));
            dataTableSchema.Columns.Add ("BaseColumnName", typeof (String));
            dataTableSchema.Columns.Add ("BaseSchemaName", typeof (String));
            dataTableSchema.Columns.Add ("BaseTableName", typeof (String));
            dataTableSchema.Columns.Add ("DataType", typeof(Type));
            dataTableSchema.Columns.Add ("AllowDBNull", typeof (Boolean));
            dataTableSchema.Columns.Add ("ProviderType", typeof (Int32));
            dataTableSchema.Columns.Add ("IsAliased", typeof (Boolean));
            dataTableSchema.Columns.Add ("IsExpression", typeof (Boolean));
            dataTableSchema.Columns.Add ("IsIdentity", typeof (Boolean));
            dataTableSchema.Columns.Add ("IsAutoIncrement", typeof (Boolean));
            dataTableSchema.Columns.Add ("IsRowVersion", typeof (Boolean));
            dataTableSchema.Columns.Add ("IsHidden", typeof (Boolean));
            dataTableSchema.Columns.Add ("IsLong", typeof (Boolean));
            dataTableSchema.Columns.Add ("IsReadOnly", typeof (Boolean));

            dataTableSchema.BeginLoadData();
            for (int i = 0; i < this.FieldCount; i += 1 ) {

                DataRow schemaRow = dataTableSchema.NewRow ();

                schemaRow["ColumnName"] = columns[i];
                schemaRow["ColumnOrdinal"] = i;
                schemaRow["ColumnSize"] = 0;
                schemaRow["NumericPrecision"] = 0;
                schemaRow["NumericScale"] = 0;
                schemaRow["IsUnique"] = false;
                schemaRow["IsKey"] = false;
                schemaRow["BaseCatalogName"] = "";
                schemaRow["BaseColumnName"] = columns[i];
                schemaRow["BaseSchemaName"] = "";
                schemaRow["BaseTableName"] = "";
                schemaRow["DataType"] = typeof(string);
                schemaRow["AllowDBNull"] = true;
                schemaRow["ProviderType"] = 0;
                schemaRow["IsAliased"] = false;
                schemaRow["IsExpression"] = false;
                schemaRow["IsIdentity"] = false;
                schemaRow["IsAutoIncrement"] = false;
                schemaRow["IsRowVersion"] = false;
                schemaRow["IsHidden"] = false;
                schemaRow["IsLong"] = false;
                schemaRow["IsReadOnly"] = false;

                dataTableSchema.Rows.Add (schemaRow);
                schemaRow.AcceptChanges();
            }
            dataTableSchema.EndLoadData();

            return dataTableSchema;
        }

        public bool NextResult ()
        {
            current_row++;

            return (current_row < rows.Count);
        }

        public bool Read ()
        {
            return NextResult ();
        }

        #endregion

        #region IDataRecord getters

        public bool GetBoolean (int i)
        {
            return Convert.ToBoolean (((object[]) rows[current_row])[i]);
        }

        public byte GetByte (int i)
        {
            return Convert.ToByte (((object[]) rows[current_row])[i]);
        }

        public long GetBytes (int i, long fieldOffset, byte[] buffer, int bufferOffset, int length)
        {
            byte[] data = (byte[])(((object[]) rows[current_row])[i]);
            if (buffer != null)
                Array.Copy (data, fieldOffset, buffer, bufferOffset, length);
            return data.LongLength - fieldOffset;
        }

        public char GetChar (int i)
        {
            return Convert.ToChar (((object[]) rows[current_row])[i]);
        }

        public long GetChars (int i, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            char[] data = (char[])(((object[]) rows[current_row])[i]);
            if (buffer != null)
                Array.Copy (data, fieldOffset, buffer, bufferOffset, length);
            return data.LongLength - fieldOffset;
        }

        public IDataReader GetData (int i)
        {
            return ((IDataReader) this [i]);
        }

        public string GetDataTypeName (int i)
        {
            if (decltypes != null && decltypes[i] != null)
                return decltypes[i];
            return "text"; // SQL Lite data type
        }

        public DateTime GetDateTime (int i)
        {
            return Convert.ToDateTime (((object[]) rows[current_row])[i]);
        }

        public decimal GetDecimal (int i)
        {
            return Convert.ToDecimal (((object[]) rows[current_row])[i]);
        }

        public double GetDouble (int i)
        {
            return Convert.ToDouble (((object[]) rows[current_row])[i]);
        }

        public Type GetFieldType (int i)
        {
            int row = current_row;
            if (row == -1 && rows.Count == 0) return typeof(string);
            if (row == -1) row = 0;
            object element = ((object[]) rows[row])[i];
            if (element != null)
                return element.GetType();
            else
                return typeof (string);

            // Note that the return value isn't guaranteed to
            // be the same as the rows are read if different
            // types of information are stored in the column.
        }

        public float GetFloat (int i)
        {
            return Convert.ToSingle (((object[]) rows[current_row])[i]);
        }

        public Guid GetGuid (int i)
        {
            object value = GetValue (i);
            if (!(value is Guid)) {
                if (value is DBNull)
                    throw new SqliteExecutionException ("Column value must not be null");
                throw new InvalidCastException ("Type is " + value.GetType ().ToString ());
            }
            return ((Guid) value);
        }

        public short GetInt16 (int i)
        {
            return Convert.ToInt16 (((object[]) rows[current_row])[i]);
        }

        public int GetInt32 (int i)
        {
            return Convert.ToInt32 (((object[]) rows[current_row])[i]);
        }

        public long GetInt64 (int i)
        {
            return Convert.ToInt64 (((object[]) rows[current_row])[i]);
        }

        public string GetName (int i)
        {
            return columns[i];
        }

        public int GetOrdinal (string name)
        {
            object v = column_names_sens[name];
            if (v == null)
                v = column_names_insens[name];
            if (v == null)
                throw new ArgumentException("Column does not exist.");
            return (int) v;
        }

        public string GetString (int i)
        {
            return (((object[]) rows[current_row])[i]).ToString();
        }

        public object GetValue (int i)
        {
            return ((object[]) rows[current_row])[i];
        }

        public int GetValues (object[] values)
        {
            int num_to_fill = System.Math.Min (values.Length, columns.Length);
            for (int i = 0; i < num_to_fill; i++) {
                if (((object[]) rows[current_row])[i] != null) {
                    values[i] = ((object[]) rows[current_row])[i];
                } else {
                    values[i] = null;
                }
            }
            return num_to_fill;
        }

        public bool IsDBNull (int i)
        {
            return (((object[]) rows[current_row])[i] == null);
        }

        #endregion
    }
}
// vi:tabstop=4:expandtab
