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
#include <gst/gconf/gconf.h>

#include "gst-player-engine.h"

static void end_of_stream_cb(GstElement *sink, GstPlayerEngine *engine);
static void gpe_pipeline_setup(GstPlayerEngine *engine, gchar **error);
static gboolean iterate_timeout(GstPlayerEngine *engine);
static gboolean end_of_stream_idle_cb(GstPlayerEngine *engine);
static gboolean error_idle_cb(GstPlayerEngine *engine);
static void error_cb(GstElement *element, GstElement *source, GError *message, 
	gchar *debug, GstPlayerEngine *engine);
static gboolean iterate_cb(GstPlayerEngine *engine);
static void state_change_cb(GstElement *play, GstElementState old_state, 
	GstElementState new_state, GstPlayerEngine *engine);

// Private Members

static void
gpe_pipeline_setup(GstPlayerEngine *engine, gchar **error)
{
	GstElement *sink;

	gst_init(NULL, NULL);

	engine->timer = g_timer_new();
	g_timer_stop(engine->timer);
	engine->timer_id = 0;

	engine->end_of_stream_idle_id = 0;
	engine->iterate_timeout_id = g_timeout_add(200, 
		(GSourceFunc)iterate_timeout, engine);

	engine->player_element = gst_element_factory_make("playbin", "play");
	if(engine->player_element == NULL) {
		*error = g_strdup(_("Failed to create a GStreamer player opbject"));
		return;
	}

	sink = gst_gconf_get_default_audio_sink();
	if(!sink) {
		*error = g_strdup(_("Could not get audio output sink"));
		return;
	}

	g_object_set(G_OBJECT(engine->player_element), "audio-sink", sink, NULL);
	
	g_signal_connect(engine->player_element, "error", 
		G_CALLBACK(error_cb), engine);

	g_signal_connect(engine->player_element, "eos", 
		G_CALLBACK(end_of_stream_cb), engine);

	g_signal_connect(engine->player_element, "state-change", 
		G_CALLBACK(state_change_cb), engine);
}

static gboolean
iterate_timeout(GstPlayerEngine *engine)
{
	if(gst_element_get_state(engine->player_element) != GST_STATE_PLAYING)
		return TRUE;
	
	if(engine->iterate_cb != NULL)
		engine->iterate_cb(engine, engine->position, engine->length);

	return TRUE;
}

static gboolean
end_of_stream_idle_cb(GstPlayerEngine *engine)
{
	g_timer_stop(engine->timer);
	g_timer_reset(engine->timer);

	engine->end_of_stream_idle_id = 0;
	
	engine->eos = TRUE;
	
	if(engine->eos_cb != NULL)
		engine->eos_cb(engine);
	
	return FALSE;
}

static void
end_of_stream_cb(GstElement *sink, GstPlayerEngine *engine)
{
	engine->end_of_stream_idle_id = 
		g_idle_add((GSourceFunc)end_of_stream_idle_cb, engine);
}

static gboolean error_idle_cb(GstPlayerEngine *engine)
{
	if(engine->error_cb != NULL) 
		engine->error_cb(engine, (const gchar *)engine->error);
	
	return FALSE;
}

static void
error_cb(GstElement *element, GstElement *source, GError *message, 
	gchar *debug, GstPlayerEngine *engine)
{
	gpe_stop(engine);
	
	if(engine->error != NULL)
		g_free(engine->error);
	
	engine->error = g_strdup(message->message);
	engine->have_error = TRUE;
		
	if(engine->error_cb != NULL)
		g_idle_add((GSourceFunc)error_idle_cb, engine);
}

static gboolean
iterate_cb(GstPlayerEngine *engine)
{
	gboolean result;
	gint64 position, length;
	GstFormat format = GST_FORMAT_TIME;
	
	if(!GST_FLAG_IS_SET(engine->player_element, GST_BIN_SELF_SCHEDULABLE)) {
		result = gst_bin_iterate(GST_BIN(engine->player_element));
	} else {
		g_usleep(100);
		result = gst_element_get_state(engine->player_element) 
			== GST_STATE_PLAYING;
	}
	
	gst_element_query(engine->player_element, GST_QUERY_POSITION, 
		&format, &position);
	gst_element_query(engine->player_element, GST_QUERY_TOTAL, 
		&format, &length);
	
	engine->position = position / 1000000000;
	engine->length = length / 1000000000;
	
	if(!result)
		engine->iterate_idle_id = 0;

	return result;
}

static void
state_change_cb(GstElement *play, GstElementState old_state, 
	GstElementState new_state, GstPlayerEngine *engine)
{
	if(old_state == GST_STATE_PLAYING) {
		if(engine->iterate_idle_id != 0) {
			g_source_remove(engine->iterate_idle_id);
			engine->iterate_idle_id = 0;
		}
	} else if(new_state == GST_STATE_PLAYING) {
		if(engine->iterate_idle_id != 0)
			g_source_remove(engine->iterate_idle_id);
		
		engine->iterate_idle_id = g_idle_add((GSourceFunc)iterate_cb, 
			engine);
	}
}

// Public Members

GstPlayerEngine *
gpe_new()
{
	GstPlayerEngine *engine;
	
	engine = g_new0(GstPlayerEngine, 1);
	gpe_pipeline_setup(engine, NULL);
	
	engine->file = NULL;
	engine->error = NULL;
	
	engine->eos_cb = NULL;
	engine->iterate_cb = NULL;
	engine->error_cb = NULL;
	
	engine->volume = 0;
	engine->position = 0;
	engine->length = 0;
	
	return engine;	
}

void
gpe_free(GstPlayerEngine *engine)
{
	if(engine != NULL)
		return;

	gpe_stop(engine);

	g_timer_destroy(engine->timer);

	if(engine->iterate_idle_id > 0)
		g_source_remove(engine->iterate_idle_id);
	
	if(engine->iterate_timeout_id > 0)
		g_source_remove(engine->iterate_timeout_id);

	g_object_unref(engine->player_element);
	
	g_free(engine->file);
	g_free(engine->error);
	g_free(engine);
}

void gpe_set_end_of_stream_handler(GstPlayerEngine *engine, 
	GpeEndOfStreamCallback cb)
{
	engine->eos_cb = cb;
}

void gpe_set_iterate_handler(GstPlayerEngine *engine, GpeIterateCallback cb)
{
	engine->iterate_cb = cb;
}

void gpe_set_error_handler(GstPlayerEngine *engine, GpeErrorCallback cb)
{
	engine->error_cb = cb;
}

gboolean
gpe_open(GstPlayerEngine *engine, const gchar *file)
{
	if(engine == NULL)
		return FALSE; 
	
	gpe_stop(engine);

	if(file == NULL)
		return FALSE;

	g_timer_stop(engine->timer);
	g_timer_reset(engine->timer);
	engine->position = 0;

	engine->file = g_strdup(file);
	g_object_set(G_OBJECT(engine->player_element), "uri", engine->file, NULL);

	return TRUE;
}

void
gpe_play(GstPlayerEngine *engine)
{
	if(engine == NULL)
		return;
	
	engine->eos = FALSE;
	
	gst_element_set_state(GST_ELEMENT(engine->player_element), 
		GST_STATE_PLAYING);

	g_timer_start(engine->timer);
}

void
gpe_stop(GstPlayerEngine *engine)
{
	if(engine == NULL)
		return;
	
	if(engine->end_of_stream_idle_id > 0) {
		g_source_remove(engine->end_of_stream_idle_id);
		engine->end_of_stream_idle_id = 0;
	}

	g_free(engine->file);
	engine->file = NULL;
	
	gpe_clear_error(engine);

	g_timer_stop(engine->timer);
	g_timer_reset(engine->timer);
	engine->position = 0;
	engine->length = 0;

	gst_element_set_state(GST_ELEMENT(engine->player_element),
		GST_STATE_READY);
}

void
gpe_pause(GstPlayerEngine *engine)
{
	if(engine == NULL)
		return;
	
	gst_element_set_state(GST_ELEMENT(engine->player_element), 
		GST_STATE_PAUSED);

	g_timer_stop(engine->timer);
	g_timer_reset(engine->timer);
}

void
gpe_set_volume(GstPlayerEngine *engine, int volume)
{
	int real_vol;
	double act_volume;

	if(engine == NULL)
		return;
	
	engine->volume = volume;
	real_vol = engine->volume;
	act_volume = CLAMP(real_vol, 0, 100) / 100.0;

	g_object_set(G_OBJECT(engine->player_element), "volume", act_volume, NULL);
}

gint
gpe_get_volume(GstPlayerEngine *engine)
{
	if(engine == NULL)
		return 0;
	
	return engine->volume;
}

void
gpe_set_position(GstPlayerEngine *engine, int position)
{
	if(engine == NULL)
		return;
	
	gst_element_seek(engine->player_element, 
		GST_SEEK_METHOD_SET | GST_SEEK_FLAG_FLUSH | GST_FORMAT_TIME,
		position * GST_SECOND);

	g_timer_reset(engine->timer);
}

gint
gpe_get_position(GstPlayerEngine *engine)
{
	if(engine == NULL)
		return -1;
	
	return engine->position;
}

gint
gpe_get_length(GstPlayerEngine *engine)
{
	if(engine == NULL)
		return -1;
	
	return engine->length;
}

gboolean
gpe_is_eos(GstPlayerEngine *engine)
{
	if(engine == NULL)
		return TRUE;
	
	return engine->eos;
}

const gchar *
gpe_get_error(GstPlayerEngine *engine)
{
	if(engine == NULL)
		return NULL;
	
	return engine->error;
}

void
gpe_clear_error(GstPlayerEngine *engine)
{
	if(engine == NULL)
		return;
	
	g_free(engine->error);
	engine->error = NULL;
	engine->have_error = FALSE;
}

gboolean
gpe_have_error(GstPlayerEngine *engine)
{
	if(engine == NULL)
		return FALSE;
	
	return engine->have_error;
}
