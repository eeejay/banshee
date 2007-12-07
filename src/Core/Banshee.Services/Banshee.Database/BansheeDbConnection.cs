//
// BansheeDbConnection.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using System.IO;
using System.Data;
using Mono.Data.Sqlite;

using Banshee.Base;
using Banshee.ServiceStack;

namespace Banshee.Database
{
    public class BansheeDbConnection : IService, IDisposable
    {
        private SqliteConnection connection;

        public BansheeDbConnection () : this (true)
        {
        }

        public BansheeDbConnection (bool connect)
        {
            if (connect) {
                Open ();
                BansheeDbFormatMigrator migrator = new BansheeDbFormatMigrator (connection);
                migrator.SlowStarted += OnMigrationSlowStarted;
                migrator.SlowPulse += OnMigrationSlowPulse;
                migrator.SlowFinished += OnMigrationSlowFinished;
                migrator.Migrate ();
            }
        }
        
        //private Gtk.Window slow_window;
        //private Gtk.ProgressBar slow_progress;
        
        private void IterateSlow ()
        {
            /*while (Gtk.Application.EventsPending ()) {
                Gtk.Application.RunIteration ();
            }*/
        }
        
        private void OnMigrationSlowStarted (string title, string message)
        {
           /* lock (this) {
                if (slow_window != null) {
                    slow_window.Destroy ();
                }
                
                Gtk.Application.Init ();
                
                slow_window = new Gtk.Window (String.Empty);
                slow_window.BorderWidth = 10;
                slow_window.WindowPosition = Gtk.WindowPosition.Center;
                slow_window.DeleteEvent += delegate (object o, Gtk.DeleteEventArgs args) {
                    args.RetVal = true;
                };
                
                Gtk.VBox box = new Gtk.VBox ();
                box.Spacing = 5;
                
                Gtk.Label title_label = new Gtk.Label ();
                title_label.Xalign = 0.0f;
                title_label.Markup = String.Format ("<b><big>{0}</big></b>",
                    GLib.Markup.EscapeText (title));
                
                Gtk.Label message_label = new Gtk.Label ();
                message_label.Xalign = 0.0f;
                message_label.Text = message;
                message_label.Wrap = true;
                
                slow_progress = new Gtk.ProgressBar ();
                
                box.PackStart (title_label, false, false, 0);
                box.PackStart (message_label, false, false, 0);
                box.PackStart (slow_progress, false, false, 0);
                
                slow_window.Add (box);
                slow_window.ShowAll ();
                
                IterateSlow ();
            }*/
        }
        
        private void OnMigrationSlowPulse (object o, EventArgs args)
        {
            /*lock (this) {
                slow_progress.Pulse ();
                IterateSlow ();
            }*/
        }
        
        private void OnMigrationSlowFinished (object o, EventArgs args)
        {
            /*lock (this) {
                slow_window.Destroy ();
                IterateSlow ();
            }*/
        }

        public void Dispose ()
        {
            Close ();
        }

        public void Open ()
        {
            lock (this) {
                if (connection != null) {
                    return;
                }

                string dbfile = DatabaseFile;
                Console.WriteLine ("Opening connection to Banshee Database: {0}", dbfile);
                connection = new SqliteConnection (String.Format ("Version=3,URI=file:{0}", dbfile));
                connection.Open ();

                Execute (@"
                    PRAGMA synchronous = OFF;
                    PRAGMA cache_size = 32768;
                ");
            }
        }

        public void Close ()
        {
            lock (this) {
                if (connection != null) {
                    connection.Close ();
                    connection = null;
                }
            }
        }

#region Convenience methods 

        public IDataReader ExecuteReader (SqliteCommand command)
        {
            if (command.Connection == null)
                command.Connection = connection;
            return command.ExecuteReader ();
        }

        public IDataReader ExecuteReader (BansheeDbCommand command)
        {
            return ExecuteReader (command.Command);
        }

        public IDataReader ExecuteReader (object command)
        {
            return ExecuteReader (new SqliteCommand (command.ToString ()));
        }

        public object ExecuteScalar (SqliteCommand command)
        {
            if (command.Connection == null)
                command.Connection = connection;
            return command.ExecuteScalar ();
        }

        public object ExecuteScalar (BansheeDbCommand command)
        {
            return ExecuteScalar (command.Command);
        }

        public object ExecuteScalar (object command)
        {
            return ExecuteScalar (new SqliteCommand (command.ToString ()));
        }

        public Int32 QueryInt32 (object command)
        {
            return Convert.ToInt32 (ExecuteScalar (command));
        }

        public int Execute (SqliteCommand command)
        {
            if (command.Connection == null)
                command.Connection = connection;
            command.ExecuteNonQuery ();
            return command.LastInsertRowID ();
        }

        public int Execute (BansheeDbCommand command)
        {
            return Execute (command.Command);
        }

        public int Execute (object command)
        {
            return Execute (new SqliteCommand (command.ToString ()));
        }

#endregion

        public string DatabaseFile {
            get {
                if (ApplicationContext.CommandLine.Contains ("db"))
                    return ApplicationContext.CommandLine["db"];

                string dbfile = Path.Combine (Path.Combine (Environment.GetFolderPath (
                    Environment.SpecialFolder.ApplicationData), 
                    "banshee"), 
                    "banshee.db"); 

                if (!File.Exists (dbfile)) {
                    string tdbfile = Path.Combine (Path.Combine (Path.Combine (Environment.GetFolderPath (
                        Environment.SpecialFolder.Personal),
                        ".gnome2"),
                        "banshee"),
                        "banshee.db");

                    if (File.Exists (tdbfile)) {
                        dbfile = tdbfile;
                    }
                }

                return dbfile;
            }
        }

        public IDbConnection Connection {
            get { return connection; }
        }
        
        string IService.ServiceName {
            get { return "DbConnection"; }
        }
    }
    
    public class BansheeDbCommand
    {
        private SqliteCommand command;

#region Properties

        public SqliteCommand Command {
            get { return command; }
        }

        public SqliteParameterCollection Parameters {
            get { return command.Parameters; }
        }

        public string CommandText {
            get { return command.CommandText; }
        }

#endregion

        public BansheeDbCommand(string command)
        {
            this.command = new SqliteCommand (command);
        }

        public BansheeDbCommand (string command, int num_params) : this (command)
        {
            for (int i = 0; i < num_params; i++) {
                Parameters.Add (new SqliteParameter ());
            }
        }

        // Recommended for commands that will be used more than once and that have named parameters.
        public BansheeDbCommand (string command, params string [] named_params) : this (command)
        {
            foreach (string param in named_params) {
                Parameters.Add (new SqliteParameter (param));
            }
        }

        public BansheeDbCommand (string command, params object [] param_values) : this (command, param_values.Length)
        {
            ApplyValues (param_values);
        }

        public void ApplyValues (params object [] param_values)
        {
            if (param_values.Length != Parameters.Count) {
                throw new ArgumentException (String.Format (
                    "Command has {0} parameters, but {1} values given.", Parameters.Count, param_values.Length
                ));
            }

            for (int i = 0; i < param_values.Length; i++) {
                Parameters[i].Value = param_values[i];
            }
        }
                
        /*public DbCommand(string command, params object [] parameters) : this(command)
        {
            for(int i = 0; i < parameters.Length;) {
                SqliteParameter param;
                
                if(parameters[i] is SqliteParameter) {
                    param = (SqliteParameter)parameters[i];
                    if(i < parameters.Length - 1 && !(parameters[i + 1] is SqliteParameter)) {
                        param.Value = parameters[i + 1];
                        i += 2;
                    } else {
                        i++;
                    }
                } else {
                    param = new SqliteParameter();
                    param.ParameterName = (string)parameters[i];
                    param.Value = parameters[i + 1];
                    i += 2;
                }
                
                Parameters.Add(param);
            }
        }
        
        public void AddParameter (object value)
        {
            SqliteParameter param = new SqliteParameter ();
            param.Value = value;
            Parameters.Add (param);
        }
        
        public void AddParameter<T>(string name, T value)
        {
            AddParameter<T>(new DbParameter<T>(name), value);
        }
        
        public void AddParameter<T>(DbParameter<T> param, T value)
        {
            param.Value = value;
            Parameters.Add(param);
        }*/
    }
}
