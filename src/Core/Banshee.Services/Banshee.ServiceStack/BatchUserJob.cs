// 
// BatchUserJob.cs
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

using Hyena.Data;

namespace Banshee.ServiceStack
{
    public class BatchUserJob : UserJob
    {
        protected string status_format;
        protected int completed;
        protected int total;

        public BatchUserJob (string title, string statusFormat, params string [] iconNames) : base (title, null, iconNames)
        {
            status_format = statusFormat;
        }

        public int Completed {
            get { return completed; }
            set {
                completed = value;
                UpdateProgress ();
                if (total > 0 && completed == total)
                    Finish ();
            }
        }

        public int Total {
            get { return total; }
            set {
                total = value;
                UpdateProgress ();
            }
        }

        protected void UpdateProgress ()
        {
            if (Total > 0) {
                FreezeUpdate ();
                Status = String.Format (status_format, completed, total);
                Progress = (double)Completed / (double)Total;
                ThawUpdate (true);
            }
        }
    }
}
