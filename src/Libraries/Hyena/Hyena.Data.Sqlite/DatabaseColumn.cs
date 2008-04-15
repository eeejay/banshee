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
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Hyena.Data.Sqlite
{
    internal abstract class AbstractDatabaseColumn<T>
    {
        
#region Dynamic Method stuff
        
        private delegate object GetDel (T target);
        private delegate void SetDel (T target, IDataReader reader, int column);
        private delegate void SetIntDel (T target, int value);
        
        private static readonly FieldInfo dateTimeMinValue = typeof (DateTime).GetField ("MinValue");
        private static readonly MethodInfo dateTimeEquals = typeof (DateTime).GetMethod ("Equals", new Type[] { typeof (DateTime), typeof (DateTime) });
        private static readonly FieldInfo timeSpanMinValue = typeof (TimeSpan).GetField ("MinValue");
        private static readonly MethodInfo timeSpanEquals = typeof (TimeSpan).GetMethod ("Equals", new Type[] { typeof (TimeSpan), typeof (TimeSpan) });
        private static readonly PropertyInfo timeSpanTotalMilliseconds = typeof (TimeSpan).GetProperty ("TotalMilliseconds");
        private static readonly MethodInfo timeSpanFromMilliseconds = typeof (TimeSpan).GetMethod ("FromMilliseconds");
        private static readonly MethodInfo fromDateTime = typeof (DateTimeUtil).GetMethod ("FromDateTime");
        private static readonly MethodInfo toDateTime = typeof (DateTimeUtil).GetMethod ("ToDateTime");
        private static readonly MethodInfo readerIsDbNull = typeof (IDataRecord).GetMethod ("IsDBNull");
        private static readonly MethodInfo readerGetInt32 = typeof (IDataRecord).GetMethod ("GetInt32");
        private static readonly MethodInfo readerGetInt64 = typeof (IDataRecord).GetMethod ("GetInt64");
        private static readonly MethodInfo readerGetString = typeof (IDataRecord).GetMethod ("GetString");
        
        private GetDel get_del;
        private GetDel get_raw_del;
        private SetDel set_del;
        private SetIntDel set_int_del;
        
#endregion
        
        private readonly FieldInfo field_info;
        private readonly PropertyInfo property_info;
        private readonly Type type;
        private readonly string column_type;
        private readonly string name;
        private readonly bool select;
        
        protected AbstractDatabaseColumn (FieldInfo field_info, AbstractDatabaseColumnAttribute attribute)
            : this (attribute, field_info, field_info.FieldType)
        {
            this.field_info = field_info;
            BuildMethods ();
        }
        
        protected AbstractDatabaseColumn (PropertyInfo property_info, AbstractDatabaseColumnAttribute attribute) :
            this (attribute, property_info, property_info.PropertyType)
        {
            if (!property_info.CanRead || (select && !property_info.CanWrite)) {
                throw new Exception (String.Format (
                    "{0}: The property {1} must have both a get and a set " +
                    "block in order to be bound to a database column.",
                    property_info.DeclaringType,
                    property_info.Name)
                );
            }
            this.property_info = property_info;
            BuildMethods ();
        }
        
        private AbstractDatabaseColumn (AbstractDatabaseColumnAttribute attribute, MemberInfo member_info, Type type)
        {
            try {
                column_type = SqliteUtils.GetType (type);
            } catch (Exception e) {
                throw new Exception(string.Format(
                    "{0}.{1}: {3}", member_info.DeclaringType, member_info.Name, e.Message));
            }
            this.type = type;
            this.name = attribute.ColumnName ?? member_info.Name;
            this.select = attribute.Select;
        }
        
#region Dynamic Method Construction
        
        private void BuildMethods ()
        {
            BuildGetMethod ();
            BuildGetRawMethod ();
            if (select) {
                BuildSetMethod ();
                if (type == typeof (int)) {
                    BuildSetIntMethod ();
                }
            }
        }
        
        private void BuildGetMethod ()
        {
            DynamicMethod method = new DynamicMethod (String.Format ("Get_{0}", name), typeof (object), new Type [] { typeof (T) }, typeof (T));
            ILGenerator il = method.GetILGenerator ();
            il.Emit (OpCodes.Ldarg_0);
            if (field_info != null) {
                il.Emit (OpCodes.Ldfld, field_info);
            } else {
                il.Emit (OpCodes.Call, property_info.GetGetMethod (true));
            }
            if (type == typeof (DateTime)) {
                il.Emit (OpCodes.Dup);
                il.Emit (OpCodes.Ldsfld, dateTimeMinValue);
                il.Emit (OpCodes.Call, dateTimeEquals);
                Label label = il.DefineLabel ();
                il.Emit (OpCodes.Brfalse, label);
                il.Emit (OpCodes.Pop);
                il.Emit (OpCodes.Ldnull);
                il.Emit (OpCodes.Ret);
                il.MarkLabel (label);
                il.Emit (OpCodes.Call, fromDateTime);
                il.Emit (OpCodes.Box, typeof (long));
            } else if (type == typeof (TimeSpan)) {
                il.Emit (OpCodes.Ldsfld, timeSpanMinValue);
                il.Emit (OpCodes.Call, timeSpanEquals);
                Label label = il.DefineLabel ();
                il.Emit (OpCodes.Brfalse_S, label);
                il.Emit (OpCodes.Ldnull);
                il.Emit (OpCodes.Ret);
                il.MarkLabel (label);
                if (field_info != null) {
                    il.Emit (OpCodes.Ldarg_0);
                    il.Emit (OpCodes.Ldflda, field_info);
                } else {
                    il.DeclareLocal (type);
                    il.Emit (OpCodes.Ldarg_0);
                    il.Emit (OpCodes.Call, property_info.GetGetMethod ());
                    il.Emit (OpCodes.Stloc_0);
                    il.Emit (OpCodes.Ldloca, 0);
                }
                il.Emit (OpCodes.Call, timeSpanTotalMilliseconds.GetGetMethod ());
                il.Emit (OpCodes.Conv_I8);
                il.Emit (OpCodes.Box, typeof (long));
            } else if (type.IsEnum) {
                il.Emit (OpCodes.Box, Enum.GetUnderlyingType (type));
            } else if (type.IsValueType) {
                il.Emit (OpCodes.Box, type);
            }
            il.Emit (OpCodes.Ret);
            get_del = (GetDel)method.CreateDelegate (typeof (GetDel));
        }
        
        private void BuildGetRawMethod ()
        {
            DynamicMethod method = new DynamicMethod (String.Format ("GetRaw_{0}", name), typeof (object), new Type [] { typeof (T) }, typeof (T));
            ILGenerator il = method.GetILGenerator ();
            il.Emit (OpCodes.Ldarg_0);
            if (field_info != null) {
                il.Emit (OpCodes.Ldfld, field_info);
            } else {
                il.Emit (OpCodes.Call, property_info.GetGetMethod (true));
            }
            if (type.IsValueType) {
                il.Emit (OpCodes.Box, type);
            }
            il.Emit (OpCodes.Ret);
            get_raw_del = (GetDel)method.CreateDelegate (typeof (GetDel));
        }
        
        private void BuildAssignmentIL (ILGenerator il)
        {
            if (field_info != null) {
                il.Emit (OpCodes.Stfld, field_info);
            } else {
                il.Emit (OpCodes.Call, property_info.GetSetMethod (true));
            }
            il.Emit (OpCodes.Ret);
        }
        
        private void BuildSetMethod ()
        {
            DynamicMethod method = new DynamicMethod (String.Format ("Set_{0}", name), null, new Type [] { typeof (T), typeof (IDataReader), typeof (int) }, typeof (T));
            ILGenerator il = method.GetILGenerator ();
            il.Emit (OpCodes.Ldarg_0);
            il.Emit (OpCodes.Ldarg_1);
            il.Emit (OpCodes.Ldarg_2);
            il.Emit (OpCodes.Callvirt, readerIsDbNull);
            Label label_not_null = il.DefineLabel ();
            il.Emit (OpCodes.Brfalse, label_not_null);
            
            if (type == typeof (DateTime)) {
                il.Emit (OpCodes.Ldsfld, dateTimeMinValue);
                Label label_end = il.DefineLabel ();
                il.Emit (OpCodes.Br, label_end);
                il.MarkLabel (label_not_null);
                il.Emit (OpCodes.Ldarg_1);
                il.Emit (OpCodes.Ldarg_2);
                il.Emit (OpCodes.Callvirt, readerGetInt64);
                il.Emit (OpCodes.Call, toDateTime);
                il.MarkLabel (label_end);
                BuildAssignmentIL (il);
            } else if (type == typeof (TimeSpan)) {
                il.Emit (OpCodes.Ldsfld, timeSpanMinValue);
                Label label_end = il.DefineLabel ();
                il.Emit (OpCodes.Br, label_end);
                il.MarkLabel (label_not_null);
                il.Emit (OpCodes.Ldarg_1);
                il.Emit (OpCodes.Ldarg_2);
                il.Emit (OpCodes.Callvirt, readerGetInt64);
                il.Emit (OpCodes.Conv_R8);
                il.Emit (OpCodes.Call, timeSpanFromMilliseconds);
                il.MarkLabel (label_end);
                BuildAssignmentIL (il);
            } else {
                il.Emit (OpCodes.Pop);
                il.Emit (OpCodes.Ret);
                il.MarkLabel (label_not_null);
                il.Emit (OpCodes.Ldarg_1);
                il.Emit (OpCodes.Ldarg_2);
                MethodInfo getter = null;
                if (type.IsValueType) {
                    Type real_type = type.IsEnum ? Enum.GetUnderlyingType (type) : type;
                    getter = real_type == typeof (int) ? readerGetInt32 : readerGetInt64;
                } else {
                    getter = readerGetString;
                }
                il.Emit (OpCodes.Callvirt, getter);
                BuildAssignmentIL (il);
            }
            set_del = (SetDel)method.CreateDelegate (typeof (SetDel));
        }
        
        private void BuildSetIntMethod ()
        {
            DynamicMethod method = new DynamicMethod (String.Format ("SetInt_{0}", name), null, new Type [] { typeof (T), typeof (int) }, typeof (T));
            ILGenerator il = method.GetILGenerator ();
            il.Emit (OpCodes.Ldarg_0);
            il.Emit (OpCodes.Ldarg_1);
            BuildAssignmentIL (il);
            set_int_del = (SetIntDel)method.CreateDelegate (typeof (SetIntDel));
        }
        
#endregion

        public object GetRawValue (T target)
        {
            return get_raw_del (target);
        }
        
        public object GetValue (T target)
        {
            return get_del (target);
        }
        
        public void SetValue (T target, IDataReader reader, int column)
        {
            set_del (target, reader, column);
        }
        
        public void SetIntValue (T target, int value)
        {
            set_int_del (target, value);
        }
        
        public void SetValue (T target, object value)
        {
            if (field_info != null) {
                field_info.SetValue (target, value);
            } else {
                property_info.SetValue (target, value, null);
            }
        }
        
        public string Name {
            get { return name; }
        }
        
        public string Type {
            get { return column_type; }
        }
    }
    
    internal sealed class DatabaseColumn<T> : AbstractDatabaseColumn<T>
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
                return SqliteUtils.BuildColumnSchema (Type, Name, attribute.DefaultValue, attribute.Constraints);
            }
        }
        
        public override bool Equals (object o)
        {
            DatabaseColumn<T> column = o as DatabaseColumn<T>;
            return o != null && column.Name.Equals (Name);
        }
        
        public override int GetHashCode ()
        {
            return Name.GetHashCode ();
        }
    }
    
    internal sealed class VirtualDatabaseColumn<T> : AbstractDatabaseColumn<T>
    {
        private VirtualDatabaseColumnAttribute attribute;
        
        public VirtualDatabaseColumn (FieldInfo field_info, VirtualDatabaseColumnAttribute attribute)
            : base (field_info, attribute)
        {
            this.attribute = attribute;
        }
        
        public VirtualDatabaseColumn (PropertyInfo property_info, VirtualDatabaseColumnAttribute attribute)
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
    
    public struct DbColumn
    {
        public readonly string Name;
        public readonly DatabaseColumnConstraints Constraints;
        public readonly string DefaultValue;
        
        public DbColumn(string name, DatabaseColumnConstraints constraints, string default_value)
        {
            Name = name;
            Constraints = constraints;
            DefaultValue = default_value;
        }
    }
}
