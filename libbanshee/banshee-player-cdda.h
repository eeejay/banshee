//
// banshee-player-cdda.h
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

#include <gst/cdda/gstcddabasesrc.h>

static GstElement *
bp_cdda_get_cdda_source (GstElement *playbin)
{
    GstElement *source = NULL;
    
    if (playbin == NULL) {
        return NULL;
    }
    
    g_object_get (playbin, "source", &source, NULL);
    
    if (source == NULL || !GST_IS_CDDA_BASE_SRC (source)) {
        if (source != NULL) {
            g_object_unref (source);
        }
        
        return NULL;
    }
    
    return source;
}

static void
bp_cdda_on_notify_source (GstElement *playbin, gpointer unknown, BansheePlayer *player)
{
    GstElement *cdda_src = NULL;
    
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (player->cdda_device == NULL) {
        return;
    }
    
    cdda_src = bp_cdda_get_cdda_source (playbin);
    if (cdda_src == NULL) {
        return;
    }
    
    // Technically don't need to check the class, since GstCddaBaseSrc elements will always have this
    if (G_LIKELY (g_object_class_find_property (G_OBJECT_GET_CLASS (cdda_src), "device"))) {
        bp_debug ("bp_cdda: setting device property on source (%s)", player->cdda_device);
        g_object_set (cdda_src, "device", player->cdda_device, NULL);
    }
    
    // If the GstCddaBaseSrc is cdparanoia, it will have this property, so set it
    if (g_object_class_find_property (G_OBJECT_GET_CLASS (cdda_src), "paranoia-mode")) {
        g_object_set (cdda_src, "paranoia-mode", 0, NULL);
    }
    
    g_object_unref (cdda_src);
}

static gboolean
bp_cdda_source_seek_to_track (GstElement *playbin, guint track)
{
    static GstFormat format = GST_FORMAT_UNDEFINED;
    GstElement *cdda_src = NULL;
    GstState state;
    
    format = gst_format_get_by_nick ("track");
    if (G_UNLIKELY (format == GST_FORMAT_UNDEFINED)) {
        return FALSE;
    }
    
    gst_element_get_state (playbin, &state, NULL, 0);
    if (state < GST_STATE_PAUSED) {
        // We can only seek if the pipeline is playing or paused, otherwise
        // we just allow playbin to do its thing, which will re-start the
        // device and start at the desired track
        return FALSE;
    }

    cdda_src = bp_cdda_get_cdda_source (playbin);
    if (G_UNLIKELY (cdda_src == NULL)) {
        return FALSE;
    }
    
    if (gst_element_seek (playbin, 1.0, format, GST_SEEK_FLAG_FLUSH, 
        GST_SEEK_TYPE_SET, track - 1, GST_SEEK_TYPE_NONE, -1)) {
        bp_debug ("bp_cdda: seeking to track %d, avoiding playbin", track);
        g_object_unref (cdda_src);
        return TRUE;
    }
    
    g_object_unref (cdda_src);
    return FALSE;
}

static gboolean
bp_cdda_handle_uri (BansheePlayer *player, const gchar *uri)
{
    // Processes URIs like cdda://<track-number>#<device-node> and overrides
    // track transitioning through playbin if playback was already happening
    // from the device node by seeking directly to the track since the disc
    // is already spinning; playbin doesn't handle CDDA URIs with device nodes
    // so we have to handle setting the device property on GstCddaBaseSrc 
    // through the notify::source signal on playbin

    const gchar *new_cdda_device;
    const gchar *p;
    
    if (player == NULL || uri == NULL || !g_str_has_prefix (uri, "cdda://")) {
        // Something is hosed or the URI isn't actually CDDA
        if (player->cdda_device != NULL) {
            bp_debug ("bp_cdda: finished using device (%s)", player->cdda_device);
            g_free (player->cdda_device);
            player->cdda_device = NULL;
        }
        
        return FALSE;
    }

    p = g_utf8_strchr (uri, -1, '#');
    if (p == NULL || strlen (p) < 2) {
        // Unset the cached device node if the URI doesn't
        // have its own valid device node
        g_free (player->cdda_device);
        player->cdda_device = NULL;
        bp_debug ("bp_cdda: invalid device node in URI (%s)", uri);
        return FALSE;
    }
    
    new_cdda_device = p + 1;
            
    if (player->cdda_device == NULL) {
        // If we weren't already playing from a CD, cache the
        // device and allow playbin to begin playing it
        player->cdda_device = g_strdup (new_cdda_device);
        bp_debug ("bp_cdda: storing device node for fast seeks (%s)", player->cdda_device);
        return FALSE;
    }
    
    if (strcmp (new_cdda_device, player->cdda_device) == 0) {
        // Parse the track number from the URI and seek directly to it
        // since we are already playing from the device; prevent playbin
        // from stopping/starting the CD, which can take many many seconds
        gchar *track_str = g_strndup (uri + 7, strlen (uri) - strlen (new_cdda_device) - 8);
        gint track_num = atoi (track_str);
        g_free (track_str);
        bp_debug ("bp_cdda: fast seeking to track on already playing device (%s)", player->cdda_device);
        
        return bp_cdda_source_seek_to_track (player->playbin, track_num);
    }
    
    // We were already playing some CD, but switched to a different device node, 
    // so unset and re-cache the new device node and allow playbin to do its thing
    bp_debug ("bp_cdda: switching devices for CDDA playback (from %s, to %s)", player->cdda_device, new_cdda_device);
    g_free (player->cdda_device);
    player->cdda_device = g_strdup(new_cdda_device);
    
    return FALSE;
}
