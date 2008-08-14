//
// banshee-player-replaygain.c
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

#include <math.h>
#include "banshee-player-replaygain.h"

// ---------------------------------------------------------------------------
// Private Functions
// ---------------------------------------------------------------------------

static inline void
bp_replaygain_debug (BansheePlayer *player)
{
    gint i;
    for (i = 0; i < 11; i++) {
       printf ("%g ", player->volume_scale_history[i]);
    }
    printf ("\n");
}

static void
bp_replaygain_update_pipeline (BansheePlayer *player, 
    gdouble album_gain, gdouble album_peak,
    gdouble track_gain, gdouble track_peak)
{
    gdouble gain = album_gain == 0.0 ? album_gain : track_gain;
    gdouble peak = album_peak == 0.0 ? album_peak : track_peak;
    gdouble scale = 0.0;
    
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (gain == 0.0) {
        gint i;
        player->current_scale_from_history = TRUE;
        // Compute the average scale from history
        for (i = 1; i <= 10; i++) {
            scale += player->volume_scale_history[i] / 10.0;
        }
    } else {
        player->current_scale_from_history = FALSE;
        scale = pow (10.0, gain / 20.0);
        
        if (peak != 0.0 && scale * peak > 1.0) {
            scale = 1.0 / peak;
        }
        
        if (scale > 15.0) {
            scale = 15.0;
        }
    }
    
    player->volume_scale_history[0] = scale;
    _bp_replaygain_update_volume (player);
}

// ---------------------------------------------------------------------------
// Internal Functions
// ---------------------------------------------------------------------------

void
_bp_replaygain_process_tag (BansheePlayer *player, const gchar *tag_name, const GValue *value)
{
    if (strcmp (tag_name, GST_TAG_ALBUM_GAIN) == 0) {
        player->album_gain = g_value_get_double (value);
    } else if (strcmp (tag_name, GST_TAG_ALBUM_PEAK) == 0) {
        player->album_peak = g_value_get_double (value);
    } else if (strcmp (tag_name, GST_TAG_TRACK_GAIN) == 0) {
        player->track_gain = g_value_get_double (value);
    } else if (strcmp (tag_name, GST_TAG_TRACK_PEAK) == 0) {
        player->track_peak = g_value_get_double (value);
    }
}

void 
_bp_replaygain_handle_state_changed (BansheePlayer *player, GstState old, GstState new, GstState pending)
{
    if (old == GST_STATE_READY && new == GST_STATE_NULL && 
        pending == GST_STATE_VOID_PENDING && player->volume_scale_history_shift) {
        
        memmove (player->volume_scale_history + 1, 
            player->volume_scale_history, sizeof (gdouble) * 10);
            
        if (player->current_scale_from_history) {
            player->volume_scale_history[1] = 1.0;
        }
        
        player->volume_scale_history[0] = 1.0;
        player->volume_scale_history_shift = FALSE;
        
        player->album_gain = player->album_peak = 0.0;
        player->track_gain = player->track_peak = 0.0;
    } else if (old == GST_STATE_READY && new == GST_STATE_PAUSED && 
        pending == GST_STATE_PLAYING &&  !player->volume_scale_history_shift) {
        
        player->volume_scale_history_shift = TRUE;
        
        bp_replaygain_update_pipeline (player, 
            player->album_gain, player->album_peak, 
            player->track_gain, player->track_peak);
    }
}

void
_bp_replaygain_update_volume (BansheePlayer *player)
{
    GParamSpec *volume_spec;
    GValue value = { 0, };
    gdouble scale;
    
    if (player == NULL || player->playbin == NULL) {
        return;
    }
    
    scale = player->replaygain_enabled ? player->volume_scale_history[0] : 1.0;
    
    volume_spec = g_object_class_find_property (G_OBJECT_GET_CLASS (player->playbin), "volume");
    g_value_init (&value, G_TYPE_DOUBLE);
    g_value_set_double (&value, player->current_volume * scale);
    g_param_value_validate (volume_spec, &value);
    
    if (player->replaygain_enabled) {
        bp_debug ("scaled volume: %f (ReplayGain) * %f (User) = %f", scale, player->current_volume, 
            g_value_get_double (&value));
    }
    
    g_object_set_property (G_OBJECT (player->playbin), "volume", &value);
    g_value_unset (&value);
}

// ---------------------------------------------------------------------------
// Public Functions
// ---------------------------------------------------------------------------

P_INVOKE void
bp_replaygain_set_enabled (BansheePlayer *player, gboolean enabled)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    player->replaygain_enabled = enabled;
    bp_debug ("%s ReplayGain", enabled ? "Enabled" : "Disabled");
    _bp_replaygain_update_volume (player);
}

P_INVOKE gboolean
bp_replaygain_get_enabled (BansheePlayer *player)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), FALSE);
    return player->replaygain_enabled;
}
