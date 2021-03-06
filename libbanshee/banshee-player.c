//
// banshee-player.c
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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

#include "banshee-player-private.h"
#include "banshee-player-pipeline.h"
#include "banshee-player-cdda.h"
#include "banshee-player-missing-elements.h"
#include "banshee-player-replaygain.h"

// ---------------------------------------------------------------------------
// Private Functions
// ---------------------------------------------------------------------------

static gboolean
bp_iterate_timeout_handler (BansheePlayer *player)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    
    if(player->iterate_cb != NULL) {
        player->iterate_cb (player);
    }
    
    return TRUE;
}

static void
bp_iterate_timeout_start (BansheePlayer *player)
{
    g_return_if_fail (IS_BANSHEE_PLAYER(player));

    if (player->iterate_timeout_id == 0) {
        player->iterate_timeout_id = g_timeout_add (200, 
            (GSourceFunc)bp_iterate_timeout_handler, player);
    }
}

static void
bp_iterate_timeout_stop (BansheePlayer *player)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (player->iterate_timeout_id != 0) {
        g_source_remove (player->iterate_timeout_id);
        player->iterate_timeout_id = 0;
    }
}

static void
bp_pipeline_set_state (BansheePlayer *player, GstState state)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (state == GST_STATE_NULL || state == GST_STATE_PAUSED) {
        bp_iterate_timeout_stop (player);
    }
    
    if (GST_IS_ELEMENT (player->playbin)) {
        player->target_state = state;
        gst_element_set_state (player->playbin, state);
    }
    
    if (state == GST_STATE_PLAYING) {
        bp_iterate_timeout_start (player);
    }
}

// ---------------------------------------------------------------------------
// Public Functions
// ---------------------------------------------------------------------------

P_INVOKE void
bp_destroy (BansheePlayer *player)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (player->mutex != NULL) {
        g_mutex_free (player->mutex);
    }
    
    if (player->cdda_device != NULL) {
        g_free (player->cdda_device);
    }
    
    _bp_pipeline_destroy (player);
    _bp_missing_elements_destroy (player);
    
    memset (player, 0, sizeof (BansheePlayer));
    
    g_free (player);
    player = NULL;
    
    bp_debug ("bp_destroy: disposed player");
}

P_INVOKE BansheePlayer *
bp_new ()
{
    BansheePlayer *player = g_new0 (BansheePlayer, 1);
    
    player->mutex = g_mutex_new ();
    
    _bp_replaygain_init (player); 
    
    return player;
}

P_INVOKE gboolean
bp_initialize_pipeline (BansheePlayer *player)
{
    return _bp_pipeline_construct (player);
}

P_INVOKE gboolean
bp_open (BansheePlayer *player, const gchar *uri)
{
    GstState state;
    
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    
    // Build the pipeline if we need to
    if (player->playbin == NULL && !_bp_pipeline_construct (player)) {
        return FALSE;
    }

    // Give the CDDA code a chance to intercept the open request
    // in case it is able to perform a fast seek to a track
    if (_bp_cdda_handle_uri (player, uri)) {
        return TRUE;
    } else if (player->playbin == NULL) {
        return FALSE;
    }
    
    // Set the pipeline to the proper state
    gst_element_get_state (player->playbin, &state, NULL, 0);
    if (state >= GST_STATE_PAUSED) {
        player->target_state = GST_STATE_READY;
        gst_element_set_state (player->playbin, GST_STATE_READY);
    }
    
    // Pass the request off to playbin
    g_object_set (G_OBJECT (player->playbin), "uri", uri, NULL);
    
    return TRUE;
}

P_INVOKE void
bp_stop (BansheePlayer *player, gboolean nullstate)
{
    // Some times "stop" really means "pause", particularly with
    // CDDA track transitioning; a NULL state will release resources
    GstState state = nullstate ? GST_STATE_NULL : GST_STATE_PAUSED;
    
    if (!nullstate && player->cdda_device == NULL) {
        // only allow going to PAUSED if we're playing CDDA
        state = GST_STATE_NULL;
    }
    
    bp_debug ("bp_stop: setting state to %s", 
        state == GST_STATE_NULL ? "GST_STATE_NULL" : "GST_STATE_PAUSED");
    
    bp_pipeline_set_state (player, state);
}

P_INVOKE void
bp_pause (BansheePlayer *player)
{
    bp_pipeline_set_state (player, GST_STATE_PAUSED);
}

P_INVOKE void
bp_play (BansheePlayer *player)
{
    bp_pipeline_set_state (player, GST_STATE_PLAYING);
}

P_INVOKE gboolean
bp_set_position (BansheePlayer *player, guint64 time_ms)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    
    if (player->playbin == NULL || !gst_element_seek (player->playbin, 1.0, 
        GST_FORMAT_TIME, GST_SEEK_FLAG_FLUSH,
        GST_SEEK_TYPE_SET, time_ms * GST_MSECOND, 
        GST_SEEK_TYPE_NONE, GST_CLOCK_TIME_NONE)) {
        g_warning ("Could not seek in stream");
        return FALSE;
    }
    
    return TRUE;
}

P_INVOKE guint64
bp_get_position (BansheePlayer *player)
{
    static GstFormat format = GST_FORMAT_TIME;
    gint64 position;

    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), 0);

    if (player->playbin != NULL && gst_element_query_position (player->playbin, &format, &position)) {
        return position / GST_MSECOND;
    }
    
    return 0;
}

P_INVOKE guint64
bp_get_duration (BansheePlayer *player)
{
    static GstFormat format = GST_FORMAT_TIME;
    gint64 duration;

    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), 0);

    if (player->playbin != NULL && gst_element_query_duration (player->playbin, &format, &duration)) {
        return duration / GST_MSECOND;
    }
    
    return 0;
}

P_INVOKE gboolean
bp_can_seek (BansheePlayer *player)
{
    GstQuery *query;
    gboolean can_seek = TRUE;
    
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    
    if (player->playbin == NULL) {
        return FALSE;
    }
    
    query = gst_query_new_seeking (GST_FORMAT_TIME);
    if (!gst_element_query (player->playbin, query)) {
        // This will probably fail, 100% of the time, because it's apparently 
        // very unimplemented in GStreamer... when it's fixed
        // we will return FALSE here, and show the warning
        // g_warning ("Could not query pipeline for seek ability");
        return bp_get_duration (player) > 0;
    }
    
    gst_query_parse_seeking (query, NULL, &can_seek, NULL, NULL);
    gst_query_unref (query);
    
    return can_seek && bp_get_duration (player) > 0;
}

P_INVOKE void
bp_set_volume (BansheePlayer *player, gdouble volume)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    player->current_volume = CLAMP (volume, 0.0, 1.0);
    _bp_replaygain_update_volume (player);
}

P_INVOKE gdouble
bp_get_volume (BansheePlayer *player)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), 0.0);
    return player->current_volume;
}

P_INVOKE gboolean
bp_get_pipeline_elements (BansheePlayer *player, GstElement **playbin, GstElement **audiobin, GstElement **audiotee)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    
    *playbin = player->playbin;
    *audiobin = player->audiobin;
    *audiotee = player->audiotee;
    
    return TRUE;
}

P_INVOKE void
bp_set_application_gdk_window(BansheePlayer *player, GdkWindow *window)
{
    player->window = window;
}

P_INVOKE void
bp_set_eos_callback (BansheePlayer *player, BansheePlayerEosCallback cb)
{
    SET_CALLBACK(eos_cb);
}

P_INVOKE void
bp_set_error_callback (BansheePlayer *player, BansheePlayerErrorCallback cb)
{
    SET_CALLBACK (error_cb);
}

P_INVOKE void
bp_set_state_changed_callback (BansheePlayer *player, BansheePlayerStateChangedCallback cb)
{
    SET_CALLBACK (state_changed_cb);
}

P_INVOKE void
bp_set_iterate_callback (BansheePlayer *player, BansheePlayerIterateCallback cb)
{
    SET_CALLBACK (iterate_cb);
}

P_INVOKE void
bp_set_buffering_callback (BansheePlayer *player, BansheePlayerBufferingCallback cb)
{
    SET_CALLBACK(buffering_cb);
}

P_INVOKE void
bp_set_tag_found_callback (BansheePlayer *player, BansheePlayerTagFoundCallback cb)
{
    SET_CALLBACK (tag_found_cb);
}

P_INVOKE void
bp_get_error_quarks (GQuark *core, GQuark *library, GQuark *resource, GQuark *stream)
{
    *core = GST_CORE_ERROR;
    *library = GST_LIBRARY_ERROR;
    *resource = GST_RESOURCE_ERROR;
    *stream = GST_STREAM_ERROR;
}
