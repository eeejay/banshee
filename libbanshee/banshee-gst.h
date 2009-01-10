//
// banshee-gst.h
//
// Author:
//   Aaron Bockover <abockover@novell.com>
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

#ifndef _BANSHEE_GST_H
#define _BANSHEE_GST_H

#include <gst/gst.h>

gboolean  banshee_is_debugging ();
guint     banshee_get_version_number ();

void      banshee_log_debug (const gchar *component, const gchar *format, ...);

typedef gboolean (* BansheeGstBinIterateCallback) (GstBin *bin, GType type, gpointer bin_element, gpointer data);
gboolean  banshee_gst_bin_iterate_all_by_interface (GstBin *bin, GType type, BansheeGstBinIterateCallback callback, gpointer data);
gboolean  banshee_gst_bin_iterate_recurse (GstBin *bin, BansheeGstBinIterateCallback callback, gpointer data);

#endif /* _BANSHEE_GST_H */
