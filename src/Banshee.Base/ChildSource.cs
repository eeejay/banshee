/***************************************************************************
 *  ChildSource.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Lukas Lipka <lukas@pmad.net>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
 
namespace Banshee.Sources
{
    public class ChildSource : Source
    {
        private Source parent;	    

        protected ChildSource(string name, int position) : base(name, position)
        {
        }

        public override void AddChildSource(ChildSource source)
        {
            throw new Exception("Cannot add a child source to a child source!");
        }

        public override void RemoveChildSource(ChildSource source)
        {
            throw new Exception("Cannot remove a child source from a child source!");
        }

        public void SetParentSource(Source source)
        {
            parent = source;
        }

        public Source Parent {
            get { return parent; }
       }
    }
}
