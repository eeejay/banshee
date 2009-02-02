//
// DbIteratorJob.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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
using System.IO;
using System.Threading;

using Mono.Unix;
using Mono.Addins;

using Hyena;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Sources;
using Banshee.Kernel;
using Banshee.Metadata;
using Banshee.MediaEngine;
using Banshee.ServiceStack;
using Banshee.Library;

namespace Banshee.ServiceStack
{
    public abstract class DbIteratorJob : UserJob
    {
        private Thread thread;

        private HyenaSqliteCommand count_command;
        private HyenaSqliteCommand select_command;
        private int current_count;

        protected HyenaSqliteCommand CountCommand {
            set { count_command = value; }
        }

        protected HyenaSqliteCommand SelectCommand {
            set { select_command = value; }
        }

        public DbIteratorJob (string name) : base (name)
        {
        }

        public void RunAsync ()
        {
            thread = ThreadAssist.Spawn (Run);
            //thread.Name = Title;
        }

        public void Run ()
        {
            int total = ServiceManager.DbConnection.Query<int> (count_command);
            if (total > 0) {
                Init ();
                Iterate ();
            }
        }

        protected void Iterate ()
        {
            if (IsCancelRequested) {
                Hyena.Log.DebugFormat ("{0} cancelled", Title);
                Cleanup ();
                return;
            }

            int total = current_count + ServiceManager.DbConnection.Query<int> (count_command);
            try {
                using (HyenaDataReader reader = new HyenaDataReader (ServiceManager.DbConnection.Query (select_command))) {
                    if (reader.Read ()) {
                        IterateCore (reader);
                    } else {
                        Cleanup ();
                    }
                }
            } catch (Exception e) {
                Log.Exception (e);
            } finally {
                Progress = (double) current_count / (double) total;
                current_count++;
            }
        }

        protected virtual void Init ()
        {
        }

        protected virtual void Cleanup ()
        {
            Finish ();
        }
   
        protected abstract void IterateCore (HyenaDataReader reader);
    }
}
