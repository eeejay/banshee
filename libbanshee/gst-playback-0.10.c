/***************************************************************************
 *  gst-player-engine.c
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
 *  DEALINGS IN THE SOFTWARE.p
 */
 
#include <stdlib.h>
#include <string.h>
#include <string.h>
#include <math.h>

#include <glib.h>
#include <glib/gi18n.h>
#include <gst/gst.h>

#include "gst-playback.h"
#include "gst-misc.h"

GstPlayerEngine *
gpe_new()
{
    return (GstPlayerEngine *)g_new0(GstPlayerEngine, 1);    
}

void
gpe_free(GstPlayerEngine *engine)
{
    g_free(engine);
}

void gpe_set_end_of_stream_handler(GstPlayerEngine *engine, 
    GpeEndOfStreamCallback cb)
{
}

void gpe_set_iterate_handler(GstPlayerEngine *engine, GpeIterateCallback cb)
{
}

void gpe_set_error_handler(GstPlayerEngine *engine, GpeErrorCallback cb)
{
}

gboolean
gpe_open(GstPlayerEngine *engine, const gchar *file)
{
    return FALSE;
}

void
gpe_play(GstPlayerEngine *engine)
{
}

void
gpe_stop(GstPlayerEngine *engine)
{
}

void
gpe_pause(GstPlayerEngine *engine)
{
}

void
gpe_set_volume(GstPlayerEngine *engine, int volume)
{
}

gint
gpe_get_volume(GstPlayerEngine *engine)
{
    return 0;
}

void
gpe_set_position(GstPlayerEngine *engine, int position)
{
}

gint
gpe_get_position(GstPlayerEngine *engine)
{
    return 0;
}

gint
gpe_get_length(GstPlayerEngine *engine)
{
    return -1;
}

gboolean
gpe_is_eos(GstPlayerEngine *engine)
{
    return FALSE;
}

const gchar *
gpe_get_error(GstPlayerEngine *engine)
{
    return NULL;
}

void
gpe_clear_error(GstPlayerEngine *engine)
{
}

gboolean
gpe_have_error(GstPlayerEngine *engine)
{
    return FALSE;
}
