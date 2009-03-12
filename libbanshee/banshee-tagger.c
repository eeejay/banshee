//
// banshee-tagger.c
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

#ifdef HAVE_CONFIG_H
#  include "config.h"
#endif

#include <glib/gstdio.h>

#include "banshee-tagger.h"

// ---------------------------------------------------------------------------
// Private Functions
// ---------------------------------------------------------------------------

static void
bt_tag_list_foreach (const GstTagList *list, const gchar *tag, gpointer userdata)
{
    gint i, tag_count;
    
    tag_count  = gst_tag_list_get_tag_size (list, tag);
    g_printf ("Found %d '%s' tag%s:", tag_count, tag, tag_count == 1 ? "" : "s");
    for (i = 0; i < tag_count; i++) {
        const GValue *value;
        gchar *value_str;
        gchar *padding = tag_count == 1 ? " " : "    ";
        
        value = gst_tag_list_get_value_index (list, tag, i);
        if (value == NULL) {
            g_printf ("%s(null)\n", padding);
            continue;
        }
        
        value_str = g_strdup_value_contents (value);
        g_printf ("%s%s\n", padding, value_str);
        g_free (value_str);
    }
}

// ---------------------------------------------------------------------------
// Internal Functions
// ---------------------------------------------------------------------------

GstTagList *
bt_tag_list_new ()
{
    return gst_tag_list_new ();
}

void
bt_tag_list_free (GstTagList *list)
{
    gst_tag_list_free (list);
}

void
bt_tag_list_add_value (GstTagList *list, const gchar *tag_name, const GValue *value)
{
    gst_tag_list_add_values (list, GST_TAG_MERGE_REPLACE, tag_name, value, NULL);
}

void
bt_tag_list_add_date (GstTagList *list, gint year, gint month, gint day)
{
    GDate *date;
    
    if (!g_date_valid_dmy (day, month, year)) {
        return;
    }
    
    date = g_date_new ();
    g_date_clear (date, 1);
    g_date_set_dmy (date, day, month, year);
    
    gst_tag_list_add (list, GST_TAG_MERGE_REPLACE, GST_TAG_DATE, date, NULL);
}

void
bt_tag_list_dump (const GstTagList *list)
{
    gst_tag_list_foreach (list, bt_tag_list_foreach, NULL);
}
