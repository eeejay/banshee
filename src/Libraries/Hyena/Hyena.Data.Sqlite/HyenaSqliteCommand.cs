//
// HyenaSqliteCommand.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
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
using System.IO;
using System.Data;
using System.Text;
using System.Threading;

// NOTE: Mono.Data.Sqlite has serious threading issues.  You cannot access
//       its results from any thread but the one the SqliteConnection belongs to.
//       That is why we still use Mono.Data.SqliteClient.
using Mono.Data.SqliteClient;

namespace Hyena.Data.Sqlite
{
    public class HyenaSqliteCommand
    {
        protected object result = null;
        private Exception execution_exception = null;
        private bool finished = false;

        private string command;
        private string command_format = null;
        private string command_formatted = null;
        private int parameter_count = 0;
        private object [] current_values;

#region Properties

        public string Text {
            get { return command; }
        }

        private HyenaCommandType command_type;
        internal HyenaCommandType CommandType {
            get { return command_type; }
            set { command_type = value; }
        }

#endregion

        public HyenaSqliteCommand (string command)
        {
            this.command = command;
        }

        public HyenaSqliteCommand (string command, params object [] param_values)
        {
            this.command = command;
            ApplyValues (param_values);
        }

        internal void Execute (SqliteConnection connection)
        {
            if (finished) {
                throw new Exception ("Command is already set to finished; result needs to be claimed before command can be rerun");
            }

            execution_exception = null;
            result = null;

            SqliteCommand sql_command = new SqliteCommand (CurrentSqlText ());
            sql_command.Connection = connection;
            //Log.DebugFormat ("Executing {0}", sql_command.CommandText);

            try {
                switch (command_type) {
                    case HyenaCommandType.Reader:
                        result = sql_command.ExecuteReader ();
                        break;

                    case HyenaCommandType.Scalar:
                        result = sql_command.ExecuteScalar ();
                        break;

                    case HyenaCommandType.Execute:
                    default:
                        sql_command.ExecuteNonQuery ();
                        result = sql_command.LastInsertRowID ();
                        break;
                }
            } catch (Exception e) {
                Log.DebugFormat (String.Format ("Exception executing command: {0}", Text), e.ToString ()); 
                execution_exception = e;
            }

            finished = true;
        }

        internal object WaitForResult (HyenaSqliteConnection conn)
        {
            while (!finished) {
                conn.ResultReadySignal.WaitOne ();
            }

            object ret = result;
            
            // Reset to false in case run again
            finished = false;

            conn.ClaimResult ();

            if (execution_exception != null) {
                throw execution_exception;
            }
            
            return ret;
        }

        public HyenaSqliteCommand ApplyValues (params object [] param_values)
        {
            if (command_format == null) {
                CreateParameters ();
            }

            if (param_values.Length != parameter_count) {
                throw new ArgumentException (String.Format (
                    "Command has {0} parameters, but {1} values given.", parameter_count, param_values.Length
                ));
            }

            for (int i = 0; i < parameter_count; i++) {
                if (param_values[i] is string) {
                    param_values[i] = String.Format ("'{0}'", (param_values[i] as string).Replace ("'", "''"));
                } else if (param_values[i] is DateTime) {
                    DateTime dt = (DateTime) param_values[i];
                    if (dt == DateTime.MinValue) {
                        param_values[i] = "NULL";
                    } else {
                        DateTimeUtil.FromDateTime ((DateTime) param_values[i]);
                    }
                } else if (param_values[i] == null) {
                    param_values[i] = "NULL";
                }
            }

            current_values = param_values;
            command_formatted = null;
            return this;
        }

        private string CurrentSqlText ()
        {
            if (command_format == null) {
                return command;
            }

            if (command_formatted == null) {
                command_formatted = String.Format (command_format, current_values);
            }

            return command_formatted;
        }

        private void CreateParameters ()
        {
            StringBuilder sb = new StringBuilder ();
            foreach (char c in command) {
                if (c == '?') {
                    sb.Append ('{');
                    sb.Append (parameter_count++);
                    sb.Append ('}');
                } else {
                    sb.Append (c);
                }
            }
            command_format = sb.ToString ();
        }
    }
}
