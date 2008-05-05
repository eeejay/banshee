//
// BansheeTests.cs
//
// Author:
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
using System.Reflection;
using System.Threading;

using NUnit.Framework;

using Hyena;
using Mono.Addins;

public abstract class BansheeTests
{
    public static string Pwd;
    static BansheeTests () {
        Hyena.Log.Debugging = true;
        Pwd = Mono.Unix.UnixDirectoryInfo.GetCurrentDirectory ();
        AddinManager.Initialize (Pwd + "/../bin/");
    }

    public delegate void TestRunner<T> (T item);
    public static void AssertForEach<T> (System.Collections.Generic.IEnumerable<T> objects, TestRunner<T> runner)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder ();
        foreach (T o in objects) {
            try { runner (o); }
            catch (AssertionException e) { sb.AppendFormat ("Failed processing {0}: {1}\n", o, e.Message); }
            catch (Exception e) { sb.AppendFormat ("Failed processing {0}: {1}\n", o, e.ToString ()); }
        }

        if (sb.Length > 0)
            Assert.Fail ("\n" + sb.ToString ());
    }

    private static Thread main_loop;
    public static void StartBanshee ()
    {
        if (main_loop != null) {
            Hyena.Log.Debug ("Main loop not null, not starting");
            return;
        }

        System.IO.Directory.CreateDirectory (Pwd + "/tmp");
        Banshee.Base.ApplicationContext.CommandLine["db"] = Pwd + "/tmp/banshee.db";
        Banshee.Base.ApplicationContext.CommandLine["uninstalled"] = String.Empty;

        main_loop = new Thread (StartNereid);
        main_loop.IsBackground = false;
        main_loop.Start ();
    }

    private static void StartNereid ()
    {
        Banshee.Gui.GtkBaseClient.Entry<Nereid.Client> ();
    }

    public static void StopBanshee ()
    {
        Banshee.Base.ThreadAssist.ProxyToMain (delegate {
            Banshee.ServiceStack.Application.Shutdown ();
            Banshee.IO.Directory.Delete (Pwd + "/tmp", true);
        });

        main_loop.Join ();
        main_loop = null;
    }
}
