/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  EllipsizeLabel.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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
using System.Runtime.InteropServices;
using GLib;
using Gtk;

namespace Banshee.Widgets
{
	public class EllipsizeLabel : Label
	{
		[DllImport("libgobject-2.0-0.dll")]
		static extern IntPtr g_type_class_peek (IntPtr gtype);

		[DllImport("libgobject-2.0-0.dll")]
		static extern IntPtr g_object_class_find_property (IntPtr klass, string name);

		[DllImport("libgobject-2.0-0.dll")]
		static extern void g_object_set(IntPtr obj, string property, int value, IntPtr nullarg);

		public EllipsizeLabel() : base ("")
		{
			GType gtype = (GType)typeof(Label);
			IntPtr klass = g_type_class_peek(gtype.Val);
			IntPtr property = g_object_class_find_property(klass, "ellipsize");

			if (property != IntPtr.Zero)
				g_object_set(Handle, "ellipsize", 3, IntPtr.Zero);
		}
		
		public EllipsizeLabel(string label) : this()
		{
			LabelProp = label;
		}
	}
}
