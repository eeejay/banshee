#include <stdio.h>
#include <stdlib.h>
#include <glib.h>
#include <glib/gstdio.h>

#include <gst/gst.h>

#define IS_GST_PLAYBACK(e) (e != NULL)
#define SET_CALLBACK(cb_name) { if(engine != NULL) { engine->cb_name = cb; } }

typedef struct GstPlayback GstPlayback;

typedef void (* GstPlaybackEosCallback) (GstPlayback *engine);
typedef void (* GstPlaybackErrorCallback) (GstPlayback *engine, 
    const gchar * error, const gchar *debug);
typedef void (* GstPlaybackStateChangedCallback) (
    GstPlayback *engine, GstState old_state, 
    GstState new_state, GstState pending_state);
typedef void (* GstPlaybackIterateCallback) (GstPlayback *engine);

struct GstPlayback {
    GstElement *playbin;
    guint iterate_timeout_id;
    GstPlaybackEosCallback eos_cb;
    GstPlaybackErrorCallback error_cb;
    GstPlaybackStateChangedCallback state_changed_cb;
    GstPlaybackIterateCallback iterate_cb;
};

// private methods

static gboolean
gst_playback_bus_callback(GstBus *bus, GstMessage *message, gpointer data)
{
    GstPlayback *engine = (GstPlayback *)data;

    g_return_val_if_fail(IS_GST_PLAYBACK(engine), FALSE);

    switch(GST_MESSAGE_TYPE(message)) {
        case GST_MESSAGE_ERROR: {
            GError *error;
            gchar *debug;
            
            if(engine->error_cb != NULL) {
                gst_message_parse_error(message, &error, &debug);
                engine->error_cb(engine, error->message, debug);
                g_error_free(error);
                g_free(debug);
            }
            
            break;
        }        
        case GST_MESSAGE_EOS:
            if(engine->eos_cb != NULL) {
                engine->eos_cb(engine);
            }
            break;
        case GST_MESSAGE_STATE_CHANGED: {
            GstState old, new, pending;
            gst_message_parse_state_changed(message, &old, &new, &pending);
            if(engine->state_changed_cb != NULL) {
                engine->state_changed_cb(engine, old, new, pending);
            }
            break;
        }
        default:
            break;
    }
    
    return TRUE;
}

static gboolean 
gst_playback_construct(GstPlayback *engine)
{
    g_return_val_if_fail(IS_GST_PLAYBACK(engine), FALSE);
    
    engine->playbin = gst_element_factory_make("playbin", "playbin");
    
    g_return_val_if_fail(engine->playbin != NULL, FALSE);
    
    gst_bus_add_watch(gst_pipeline_get_bus(GST_PIPELINE(engine->playbin)), 
        gst_playback_bus_callback, engine);
        
    return TRUE;
}

static gboolean
gst_playback_iterate_timeout(GstPlayback *engine)
{
    g_return_val_if_fail(IS_GST_PLAYBACK(engine), FALSE);
    
    if(engine->iterate_cb != NULL) {
        engine->iterate_cb(engine);
    }
    
    return TRUE;
}

static void
gst_playback_start_iterate_timeout(GstPlayback *engine)
{
    g_return_if_fail(IS_GST_PLAYBACK(engine));

    if(engine->iterate_timeout_id != 0) {
        return;
    }
    
    engine->iterate_timeout_id = g_timeout_add(200, 
        (GSourceFunc)gst_playback_iterate_timeout, engine);
}

static void
gst_playback_stop_iterate_timeout(GstPlayback *engine)
{
    g_return_if_fail(IS_GST_PLAYBACK(engine));
    
    if(engine->iterate_timeout_id == 0) {
        return;
    }
    
    g_source_remove(engine->iterate_timeout_id);
    engine->iterate_timeout_id = 0;
}

// public methods

GstPlayback *
gst_playback_new()
{
    GstPlayback *engine = g_new0(GstPlayback, 1);
    if(!gst_playback_construct(engine)) {
        g_free(engine);
        return NULL;
    }
    
    engine->eos_cb = NULL;
    engine->error_cb = NULL;
    engine->state_changed_cb = NULL;
    engine->iterate_cb = NULL;
    
    engine->iterate_timeout_id = 0;
    
    return engine;
}

void
gst_playback_free(GstPlayback *engine)
{
    g_return_if_fail(IS_GST_PLAYBACK(engine));
    
    gst_element_set_state(engine->playbin, GST_STATE_NULL);
    gst_object_unref(GST_OBJECT(engine->playbin));
    
    g_free(engine);
    engine = NULL;
}

void
gst_playback_set_eos_callback(GstPlayback *engine, 
    GstPlaybackEosCallback cb)
{
    SET_CALLBACK(eos_cb);
}

void
gst_playback_set_error_callback(GstPlayback *engine, 
    GstPlaybackErrorCallback cb)
{
    SET_CALLBACK(error_cb);
}

void
gst_playback_set_state_changed_callback(GstPlayback *engine, 
    GstPlaybackStateChangedCallback cb)
{
    SET_CALLBACK(state_changed_cb);
}

void
gst_playback_set_iterate_callback(GstPlayback *engine, 
    GstPlaybackIterateCallback cb)
{
    SET_CALLBACK(iterate_cb);
}

void
gst_playback_open(GstPlayback *engine, const gchar *uri)
{
    g_return_if_fail(IS_GST_PLAYBACK(engine));
    g_object_set(G_OBJECT(engine->playbin), "uri", uri, NULL);
}

void
gst_playback_stop(GstPlayback *engine)
{
    g_return_if_fail(IS_GST_PLAYBACK(engine));
    gst_playback_stop_iterate_timeout(engine);
    gst_element_set_state(engine->playbin, GST_STATE_READY);
}

void
gst_playback_pause(GstPlayback *engine)
{
    g_return_if_fail(IS_GST_PLAYBACK(engine));
    gst_playback_stop_iterate_timeout(engine);
    gst_element_set_state(engine->playbin, GST_STATE_PAUSED);
}

void
gst_playback_play(GstPlayback *engine)
{
    g_return_if_fail(IS_GST_PLAYBACK(engine));
    gst_element_set_state(engine->playbin, GST_STATE_PLAYING);
    gst_playback_start_iterate_timeout(engine);
}

void
gst_playback_set_volume(GstPlayback *engine, gint volume)
{
	gdouble act_volume;

    g_return_if_fail(IS_GST_PLAYBACK(engine));
	act_volume = CLAMP(volume, 0, 100) / 100.0;
	g_object_set(G_OBJECT(engine->playbin), "volume", act_volume, NULL);
}

gint
gst_playback_get_volume(GstPlayback *engine)
{
    gint volume = -1;
    g_return_val_if_fail(IS_GST_PLAYBACK(engine), -1);
    g_object_get(engine->playbin, "volume", &volume, NULL);
    return volume;
}

void
gst_playback_set_position(GstPlayback *engine, guint64 time_ms)
{
    g_return_if_fail(IS_GST_PLAYBACK(engine));
    
    if(!gst_element_seek(engine->playbin, 1.0, 
        GST_FORMAT_TIME, GST_SEEK_FLAG_FLUSH,
        GST_SEEK_TYPE_SET, time_ms * GST_MSECOND, 
        GST_SEEK_TYPE_NONE, GST_CLOCK_TIME_NONE)) {
        g_warning("Could not seek in stream");
    }
}

guint64
gst_playback_get_position(GstPlayback *engine)
{
    GstFormat format = GST_FORMAT_TIME;
    gint64 position;

    g_return_val_if_fail(IS_GST_PLAYBACK(engine), 0);

    if(gst_element_query_position(engine->playbin, &format, &position)) {
        return position / 1000000;
    }
    
    return 0;
}

guint64
gst_playback_get_duration(GstPlayback *engine)
{
    GstFormat format = GST_FORMAT_TIME;
    gint64 duration;

    g_return_val_if_fail(IS_GST_PLAYBACK(engine), 0);

    if(gst_element_query_duration(engine->playbin, &format, &duration)) {
        return format / 1000000;
    }
    
    return 0;
}


