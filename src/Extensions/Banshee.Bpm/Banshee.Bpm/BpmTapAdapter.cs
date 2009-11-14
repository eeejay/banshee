//
// BpmTapWidget.cs
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
using System.Collections.Generic;

using Mono.Unix;

using Gtk;

using Hyena;

using Banshee.Base;

namespace Banshee.Bpm
{
    public class BpmTapAdapter
    {
        private const int NUM_PERIODS = 20;

        private int [] tap_periods = new int [NUM_PERIODS];
        private int tap_index;
        private int last_tap;
        private uint timeout_id = 0;

        private Button tap_button;

        private int avg_bpm;
        public int AverageBpm {
            get { return avg_bpm; }
        }

        public event Action<int> BpmChanged;

        public BpmTapAdapter (Button button)
        {
            tap_button = button;
            tap_button.Clicked += OnTapped;
        }

        public void Reset ()
        {
            Reset (false);
        }

        private void Reset (bool inclLabel)
        {
            for (int i = 0; i < NUM_PERIODS; i++) {
                tap_periods[i] = 0;
            }
            tap_index = 0;
            last_tap = 0;

            if (inclLabel) {
                avg_bpm = 0;
            }
        }

        private void OnTapped (object o, EventArgs args)
        {
            int now = Environment.TickCount;

            if (last_tap != 0) {
                int period = now - last_tap;

                Console.WriteLine ("{0} ms since last tap; eq {1} BPM", period, 60000/period);
                tap_periods[tap_index] = period;
                tap_index = (tap_index + 1) % (NUM_PERIODS - 1);
                CalculateAverage ();

                if (timeout_id != 0) {
                    GLib.Source.Remove (timeout_id);
                }

                // If the next tap doesn't come soon enough, Reset the tap history
                timeout_id = GLib.Timeout.Add ((uint)(1.5 * period), OnTapTimeout);
            }

            last_tap = now;
        }

        private bool OnTapTimeout ()
        {
            Console.WriteLine ("OnTapTimeout");
            Reset (false);
            timeout_id = 0;
            return false;
        }

        private void CalculateAverage ()
        {
            int sum = 0;
            int count = 0;
            for (int i = 0; i < NUM_PERIODS; i++) {
                if (tap_periods[i] > 0) {
                    sum += tap_periods[i];
                    count++;
                }
            }

            avg_bpm = 60000 * count / sum;

            Action<int> handler = BpmChanged;
            if (handler != null) {
                handler (avg_bpm);
            }
        }
    }
}
