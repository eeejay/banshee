/* ex: set ts=4: */
/***************************************************************************
 *  cd-rip.c
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

#ifdef HAVE_CONFIG_H
#  include "config.h"
#endif

#include <gst/gst.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <unistd.h>
#include <string.h>
#include <glib.h>
#include <glib/gi18n.h>

#include <gst/gst.h>
#include <gst/tag/tag.h>

#include "gst-cd-rip.h"
#include "gst-misc.h"

/* Public Methods */

GstCdRipper *
gst_cd_ripper_new(gchar *device, gint paranoia_mode, gchar *encoder_pipeline)
{
    return NULL;
}

void
gst_cd_ripper_free(GstCdRipper *ripper)
{
}

gboolean
gst_cd_ripper_rip_track(GstCdRipper *ripper, gchar *uri, gint track_number, 
    gchar *md_artist, gchar *md_album, gchar *md_title, gchar *md_genre,
    gint md_track_number, gint md_track_count, gpointer user_info)
{
    return FALSE;
}

void
gst_cd_ripper_set_progress_callback(GstCdRipper *ripper, GstCdRipperProgressCallback cb)
{
}

void
gst_cd_ripper_cancel(GstCdRipper *ripper)
{
}

gchar *
gst_cd_ripper_get_error(GstCdRipper *ripper)
{
}
