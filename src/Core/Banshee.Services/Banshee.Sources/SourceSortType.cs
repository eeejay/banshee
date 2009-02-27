//
// SourceSortType.cs
//
// Author:
//   John Millikin <jmillikin@gmail.com>
//
// Copyright (C) 2009 John Millikin
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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Mono.Unix;

using Hyena;
using Hyena.Data;
using Hyena.Query;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Configuration;
using Banshee.ServiceStack;

namespace Banshee.Sources
{
    public class SourceSortType
    {
        private string id;
        private string label;
        private SortType sort_type;
        private IComparer<Source> comparer;
        
        public SourceSortType (string id, string label, SortType sortType, IComparer<Source> comparer)
        {
            this.id = id;
            this.label = label;
            this.sort_type = sortType;
            this.comparer = comparer;
        }
        
        public string Id {
            get { return id; }
        }
        
        public string Label {
            get { return label; }
        }
        
        public SortType SortType {
            get { return sort_type; }
        }
        
        public IComparer<Source> Comparer {
            get { return comparer; }
        }
    }
}
