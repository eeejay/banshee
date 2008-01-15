//
// DatabaseColumn.cs
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
    internal abstract class AbstractDatabaseColumn
    {
        private readonly DatabaseColumnBaseAttribute attribute;
        private readonly FieldInfo field_info;
        private readonly PropertyInfo property_info;
        private readonly Type type;
        private readonly string column_type;
        private readonly string name;
        
        public AbstractDatabaseColumn (FieldInfo field_info, DatabaseColumnBaseAttribute attribute)
            : this (attribute, field_info, field_info.FieldType)
        {
            this.field_info = field_info;
        }
        
        public AbstractDatabaseColumn (PropertyInfo property_info, DatabaseColumnBaseAttribute attribute) :
            this (attribute, property_info, property_info.PropertyType)
        {
            if (!property_info.CanRead || !property_info.CanWrite) {
                throw new Exception (String.Format ("{0}: The property {1} must have both a get and a set " +
                                                  "block in order to be bound to a database column.",
                                                  property_info.DeclaringType,
                                                  property_info.Name));
            }
            this.property_info = property_info;
        }
        
        private AbstractDatabaseColumn (DatabaseColumnBaseAttribute attribute, MemberInfo member_info, Type type)
        {
            if (type.Equals (typeof (string))) {
                column_type = "TEXT";
            } else if (type.Equals (typeof (int)) || type.Equals (typeof (long))) {
                column_type = "INTEGER";
            } else {
                throw new Exception (String.Format ("{0}.{1}: The type {2} cannot be bound to a database column.",
                                                  member_info.DeclaringType,
                                                  member_info.Name,
                                                  type.FullName));
            }
            this.attribute = attribute;
            this.name = attribute.ColumnName ?? member_info.Name;
            this.type = type;
        }
        
        public object GetValue (object target)
        {
            object result;
            if (field_info != null) {
                result = field_info.GetValue (target);
            } else {
                result = property_info.GetGetMethod (true).Invoke (target, null);
            }
            return GetValue (type, result);
        }
        
        protected virtual object GetValue (Type type, object value)
        {
            return value;
        }
        
        public void SetValue (object target, IDataReader reader, int column)
        {
            if (field_info != null) {
                field_info.SetValue (target, SetValue (type, reader, column));
            } else {
                property_info.GetSetMethod (true).Invoke (target, new object[] { SetValue (type, reader, column) });
            }
        }
        
        protected virtual object SetValue (Type type, IDataReader reader, int column)
        {
            // FIXME should we insist on nullable types?
            object result;
            if (type.Equals (typeof (string))) {
                result = !reader.IsDBNull (column)
                    ? String.Intern (reader.GetString (column))
                    : null;
            } else if (type.Equals (typeof (int))) {
                result = !reader.IsDBNull (column)
                    ? reader.GetInt32 (column)
                    : 0;
            } else {
                result = !reader.IsDBNull (column)
                    ? reader.GetInt64 (column)
                    : 0;
            }
            return result;
        }
        
        public string Name {
            get { return name; }
        }
        
        public string Type {
            get { return column_type; }
        }
    }
    
    internal class DatabaseColumn : AbstractDatabaseColumn
    {
        private DatabaseColumnAttribute attribute;
        
        public DatabaseColumn (FieldInfo field_info, DatabaseColumnAttribute attribute)
            : base (field_info, attribute)
        {
            this.attribute = attribute;
        }
        
        public DatabaseColumn (PropertyInfo property_info, DatabaseColumnAttribute attribute)
            : base (property_info, attribute)
        {
            this.attribute = attribute;
        }
        
        public DatabaseColumnConstraints Constraints {
            get { return attribute.Constraints; }
        }
        
        public string DefaultValue {
            get { return attribute.DefaultValue; }
        }
        
        public string Index {
            get { return attribute.Index; }
        }
        
        public string Schema {
            get {
                StringBuilder builder = new StringBuilder ();
                builder.Append (Name);
                builder.Append (' ');
                builder.Append (Type);
                if ((attribute.Constraints & DatabaseColumnConstraints.NotNull) > 0) {
                    builder.Append (" NOT NULL");
                }
                if ((attribute.Constraints & DatabaseColumnConstraints.Unique) > 0) {
                    builder.Append (" UNIQUE");
                }
                if ((attribute.Constraints & DatabaseColumnConstraints.PrimaryKey) > 0) {
                    builder.Append (" PRIMARY KEY");
                }
                if (attribute.DefaultValue != null) {
                    builder.Append (" DEFAULT ");
                    builder.Append (attribute.DefaultValue);
                }
                return builder.ToString ();
            }
        }
        
        public override bool Equals (object o)
        {
            DatabaseColumn column = o as DatabaseColumn;
            return o != null && column.Name.Equals (Name);
        }
        
        public override int GetHashCode ()
        {
            return Name.GetHashCode ();
        }
    }
    
    internal class VirtualDatabaseColumn : AbstractDatabaseColumn
    {
        private DatabaseVirtualColumnAttribute attribute;
        
        public VirtualDatabaseColumn (FieldInfo field_info, DatabaseVirtualColumnAttribute attribute)
            : base (field_info, attribute)
        {
            this.attribute = attribute;
        }
        
        public VirtualDatabaseColumn (PropertyInfo property_info, DatabaseVirtualColumnAttribute attribute)
            : base (property_info, attribute)
        {
            this.attribute = attribute;
        }
        
        public string TargetTable {
            get { return attribute.TargetTable; }
        }
        
        public string LocalKey {
            get { return attribute.LocalKey; }
        }
        
        public string ForeignKey {
            get { return attribute.ForeignKey; }
        }
    }
}
