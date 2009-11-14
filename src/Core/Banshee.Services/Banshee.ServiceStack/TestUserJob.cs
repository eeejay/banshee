//
// TestUserJob.cs
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

namespace Banshee.ServiceStack
{
    public class TestUserJob : UserJob
    {
        private int icon_index = 0;
        private uint icon_timeout_id = 0;
        private uint initial_timeout_id = 0;
        private uint main_timeout_id = 0;
        private uint final_timeout_id = 0;
        private Random rand = new Random ();

        private string [] icon_names_go = new string [] {
            "go-next", "go-down", "go-previous", "go-up"
        };

        private string [] icon_names_rand = new string [] {
            "face-angel", "face-crying", "face-devilish", "face-glasses",
            "face-grin", "face-kiss", "face-monkey",  "face-plain",
            "face-sad", "face-smile-big", "face-smile", "face-surprise",
            "face-wink"
        };

        public TestUserJob () : base ("UserJob Test Job", "Waiting for 7.5 seconds...")
        {
            CancelRequested += OnCancelRequested;
            DelayShow = true;
            Register ();

            IconNames = new string [] { "media-eject" };

            initial_timeout_id = Application.RunTimeout (7500, delegate {
                Title = "New Title for Test Job";

                main_timeout_id = Application.RunTimeout (50, delegate {
                    Progress += 0.001;

                    if (Progress >= 0.45 && Progress <= 0.55) {
                        Status = null;
                    } else {
                        Status = String.Format ("I am {0:0.0}% complete", Progress * 100.0);
                    }

                    if (Progress >= 0.65 && Progress <= 0.75) {
                        Title = null;
                    } else if (Title == null) {
                        Title = "The final Title";
                    }

                    if (Progress >= 0.25 && Progress <= 0.35 && icon_timeout_id == 0) {
                        icon_timeout_id = Application.RunTimeout (100, delegate {
                            icon_index = (icon_index + 1) % icon_names_go.Length;
                            IconNames = new string [] { icon_names_go [icon_index] };
                            if (Progress <= 0.35) {
                                return true;
                            }
                            icon_timeout_id = 0;
                            return false;
                        });
                    }

                    if (Progress >= 0.45 && Progress <= 0.70 && icon_timeout_id == 0) {
                        icon_timeout_id = Application.RunTimeout (250, delegate {
                            icon_index = rand.Next (0, icon_names_rand.Length - 1);
                            IconNames = new string [] { icon_names_rand[icon_index] };
                            if (Progress <= 0.65) {
                                return true;
                            }
                            icon_timeout_id = 0;
                            return false;
                        });
                    }

                    CanCancel = (Progress >= 0.15 && Progress <= 0.30) || (Progress >= 0.65 && Progress <= 0.85);

                    if (Progress == 1.0) {
                        Progress = 0.0;
                        Title = "Bouncing";
                        Status = "I'm going to bounce now...";
                        final_timeout_id = Application.RunTimeout (8000, delegate {
                            Finish ();
                            return false;
                        });

                        return false;
                    }

                    return true;
                });

                return false;
            });
        }

        private void OnCancelRequested (object o, EventArgs args)
        {
            if (initial_timeout_id > 0) {
                Application.IdleTimeoutRemove (initial_timeout_id);
            }

            if (main_timeout_id > 0) {
                Application.IdleTimeoutRemove (main_timeout_id);
            }

            if (icon_timeout_id > 0) {
                Application.IdleTimeoutRemove (icon_timeout_id);
            }

            if (final_timeout_id > 0) {
                Application.IdleTimeoutRemove (final_timeout_id);
            }

            OnFinished ();
        }

        public static void SpawnLikeFish (int count)
        {
            int i = 0;
            Application.RunTimeout (2000, delegate {
                new TestUserJob ();
                return ++i < count;
            });
        }
    }
}
