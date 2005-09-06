/***************************************************************************
 *  gst-player-engine.h
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

#ifndef GST_PLAYER_ENGINE_H
#define GST_PLAYER_ENGINE_H

#include <glib.h>
#include <gst/gst.h>
#include <gst/gconf/gconf.h>

typedef void (* GpeEndOfStreamCallback) (void *engine);
typedef void (* GpeIterateCallback) (void *engine, int position, int length);
typedef void (* GpeErrorCallback) (void *engine, const gchar *error);

typedef struct {
	GstElement *player_element;

	gchar *file;
	gchar *cd_device;
	gchar *error;
	
	gboolean have_error;
	gboolean eos;
		
	gint position;
	gint length;
	gint volume;

	GTimer *timer;
	guint timer_id;
	
	guint end_of_stream_idle_id;
	guint iterate_idle_id;
	guint error_id;
	guint iterate_timeout_id;
	
	GpeEndOfStreamCallback eos_cb;
	GpeIterateCallback iterate_cb;
	GpeErrorCallback error_cb;
} GstPlayerEngine;

GstPlayerEngine *gpe_new();
void gpe_free(GstPlayerEngine *engine);

void gpe_set_end_of_stream_handler(GstPlayerEngine *engine, 
	GpeEndOfStreamCallback cb);
void gpe_set_iterate_handler(GstPlayerEngine *engine, GpeIterateCallback cb);
void gpe_set_error_handler(GstPlayerEngine *engine, GpeErrorCallback cb);

gboolean gpe_open(GstPlayerEngine *engine, const gchar *file);
void gpe_play(GstPlayerEngine *engine);
void gpe_stop(GstPlayerEngine *engine);
void gpe_pause(GstPlayerEngine *engine);
void gpe_set_volume(GstPlayerEngine *engine, int volume);
gint gpe_get_volume(GstPlayerEngine *engine);
void gpe_set_position(GstPlayerEngine *engine, int position);
gint gpe_get_position(GstPlayerEngine *engine);
gint gpe_get_length(GstPlayerEngine *engine);
gboolean gpe_is_eos(GstPlayerEngine *engine);
const gchar *gpe_get_error(GstPlayerEngine *engine);
void gpe_clear_error(GstPlayerEngine *engine);
gboolean gpe_have_error(GstPlayerEngine *engine);

#endif
