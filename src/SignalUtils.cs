/* -*- Mode: csharp; tab-width: 4; c-basic-offset: 4; indent-tabs-mode: t -*- */
/***************************************************************************
 *  SignalUtils.cs
 *
 *  Copyright (C) 2004 Tamara Roberson
 *  foxxygirltamara@gmail.com
 *  [This file was adapted from Muine]
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

namespace Banshee
{
	public sealed class SignalUtils 
	{
		// Delegates
	        public delegate void SignalDelegate    (IntPtr obj);
	        public delegate void SignalDelegatePtr (IntPtr obj, IntPtr arg);
	        public delegate void SignalDelegateInt (IntPtr obj, int    arg);
	        public delegate void SignalDelegateStr (IntPtr obj, string arg);

	        // Methods
	        // Methods :: Public
	        // Methods :: Public :: SignalConnect
		[DllImport ("libgobject-2.0-0.dll")]
		private static extern uint g_signal_connect_data (IntPtr obj, string name,
								  SignalDelegate cb, IntPtr data,
								  IntPtr p, int flags);

		[DllImport ("libgobject-2.0-0.dll")]
		private static extern uint g_signal_connect_data (IntPtr obj, string name,
								  SignalDelegatePtr cb, IntPtr data,
								  IntPtr p, int flags);

		[DllImport ("libgobject-2.0-0.dll")]
		private static extern uint g_signal_connect_data (IntPtr obj, string name,
								  SignalDelegateInt cb, IntPtr data,
								  IntPtr p, int flags);

		[DllImport ("libgobject-2.0-0.dll")]
		private static extern uint g_signal_connect_data (IntPtr obj, string name,
								  SignalDelegateStr cb, IntPtr data,
								  IntPtr p, int flags);

		// Plain								  								  
	        public static uint SignalConnect (IntPtr obj, string name, SignalDelegate cb)
	        {
	                return SignalConnect (obj, name, cb, IntPtr.Zero, IntPtr.Zero, 0);
	        }
	        
	        public static uint SignalConnect (IntPtr obj, string name, SignalDelegate cb, 
	                                          IntPtr data, IntPtr p, int flags)
	        {
	                return g_signal_connect_data (obj, name, cb, data, p, flags);
	        }

		// Ptr
	        public static uint SignalConnect (IntPtr obj, string name, SignalDelegatePtr cb)
	        {
	                return SignalConnect (obj, name, cb, IntPtr.Zero, IntPtr.Zero, 0);
	        }
	        
	        public static uint SignalConnect (IntPtr obj, string name, SignalDelegatePtr cb, 
	                                          IntPtr data, IntPtr p, int flags)
	        {
	                return g_signal_connect_data (obj, name, cb, data, p, flags);
	        }

		// Int
	        public static uint SignalConnect (IntPtr obj, string name, SignalDelegateInt cb)
	        {
	                return SignalConnect (obj, name, cb, IntPtr.Zero, IntPtr.Zero, 0);
	        }
	        
	        public static uint SignalConnect (IntPtr obj, string name, SignalDelegateInt cb, 
	                                          IntPtr data, IntPtr p, int flags)
	        {
	                return g_signal_connect_data (obj, name, cb, data, p, flags);
	        }

		// Str
	        public static uint SignalConnect (IntPtr obj, string name, SignalDelegateStr cb)
	        {
	                return SignalConnect (obj, name, cb, IntPtr.Zero, IntPtr.Zero, 0);
	        }
	        
	        public static uint SignalConnect (IntPtr obj, string name, SignalDelegateStr cb, 
	                                          IntPtr data, IntPtr p, int flags)
	        {
	                return g_signal_connect_data (obj, name, cb, data, p, flags);
	        }
	}
}
