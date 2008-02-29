//
// RateLimiter.cs
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
using GLib;

namespace Banshee.Base
{
    public class RateLimiter
    {
        public delegate void RateLimitedMethod ();

        private RateLimitedMethod method;
        private double initial_delay_ms;
        private double min_interval_ms;
        private DateTime last_executed = DateTime.MinValue;
        private uint timeout_id = 0;

        private bool requested = false;
        private double requested_interval;
        private bool executing = false;

        private string name;

        public RateLimiter (double min_interval_ms, RateLimitedMethod method) : this (0.0, min_interval_ms, method)
        {
        }

        public RateLimiter (double initial_delay_ms, double min_interval_ms, RateLimitedMethod method)
        {
            this.initial_delay_ms = initial_delay_ms;
            this.min_interval_ms = min_interval_ms;
            this.method = method;
        }

        public void Execute ()
        {
            Execute (min_interval_ms);
        }

        public void Execute (double min_interval_ms)
        {
            lock (this) {
                if (requested || timeout_id != 0) {
                    return;
                }

                if (executing) {
                    requested = true;
                    requested_interval = min_interval_ms;
                    return;
                }

                double delta = (DateTime.Now - last_executed).TotalMilliseconds;
                if (delta >= min_interval_ms) {
                    timeout_id = GLib.Timeout.Add ((uint) initial_delay_ms, OnRateLimitTimer);
                } else {
                    timeout_id = GLib.Timeout.Add ((uint) (min_interval_ms - delta), OnRateLimitTimer);
                }
            }
        }

        private bool OnRateLimitTimer ()
        {
            lock (this) {
                timeout_id = 0;
                executing = true;
            }

            //Hyena.Log.DebugFormat ("Executing method {0} from {1} in {2}", method.Method.Name, method.Method.DeclaringType, System.Threading.Thread.CurrentThread.ManagedThreadId);
            method ();

            lock (this) {
                last_executed = DateTime.Now;
                //Hyena.Log.DebugFormat ("Done executing method {0} from {1} at {2}", method.Method.Name, method.Method.DeclaringType, last_executed);
                executing = false;
            }

            if (requested) {
                requested = false;
                Execute (requested_interval);
            }

            return false;
        }
    }
}
