//
// DetailsReview.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2009 Novell, Inc.
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
using System.Linq;

using Mono.Unix;

using Hyena.Json;

namespace InternetArchive
{
    public class DetailsReview
    {
        JsonObject review;

        public DetailsReview (JsonObject review)
        {
            this.review = review;
        }

        public long Id {
            get { return review.Get<int> ("review_id"); }
        }

        public int Stars {
            get { return review.Get<int> ("stars"); }
        }

        public string Title {
            get { return review.Get<string> ("reviewtitle"); }
        }

        public string Body {
            get { return review.Get<string> ("reviewbody"); }
        }

        public string Reviewer {
            get { return review.Get<string> ("reviewer"); }
        }

        public DateTime DateReviewed {
            get { return DateTime.Parse (review.Get<string> ("reviewdate")); }
        }
    }
}
