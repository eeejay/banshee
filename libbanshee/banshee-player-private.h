//
// banshee-player-private.h
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

#ifndef _BANSHEE_PLAYER_PRIVATE_H
#define _BANSHEE_PLAYER_PRIVATE_H

#ifdef HAVE_CONFIG_H
#  include "config.h"
#endif

#include <string.h>
#include <gst/gst.h>
#include <gst/base/gstadapter.h>
#include <gdk/gdk.h>
#include <gst/fft/gstfftf32.h>

#ifdef HAVE_GST_PBUTILS
#  include <gst/pbutils/pbutils.h>
#endif

#ifdef GDK_WINDOWING_X11
#  include <gdk/gdkx.h>
#  include <gst/interfaces/xoverlay.h>
#endif

#ifdef HAVE_CLUTTER
#  include <clutter/clutter.h>
#endif

#include "banshee-gst.h"

#define P_INVOKE
#define IS_BANSHEE_PLAYER(e) (e != NULL)
#define SET_CALLBACK(cb_name) { if(player != NULL) { player->cb_name = cb; } }

#define bp_debug(x...) banshee_log_debug ("player", x)

typedef struct BansheePlayer BansheePlayer;

typedef void (* BansheePlayerEosCallback)          (BansheePlayer *player);
typedef void (* BansheePlayerErrorCallback)        (BansheePlayer *player, GQuark domain, gint code, 
                                                    const gchar *error, const gchar *debug);
typedef void (* BansheePlayerStateChangedCallback) (BansheePlayer *player, GstState old_state, 
                                                    GstState new_state, GstState pending_state);
typedef void (* BansheePlayerIterateCallback)      (BansheePlayer *player);
typedef void (* BansheePlayerBufferingCallback)    (BansheePlayer *player, gint buffering_progress);
typedef void (* BansheePlayerTagFoundCallback)     (BansheePlayer *player, const gchar *tag, const GValue *value);
typedef void (* BansheePlayerVisDataCallback)      (BansheePlayer *player, gint channels, gint samples, gfloat *data, gint bands, gfloat *spectrum);

struct BansheePlayer {
    // Player Callbacks
    BansheePlayerEosCallback eos_cb;
    BansheePlayerErrorCallback error_cb;
    BansheePlayerStateChangedCallback state_changed_cb;
    BansheePlayerIterateCallback iterate_cb;
    BansheePlayerBufferingCallback buffering_cb;
    BansheePlayerTagFoundCallback tag_found_cb;
    BansheePlayerVisDataCallback vis_data_cb;

    // Pipeline Elements
    GstElement *playbin;
    GstElement *audiotee;
    GstElement *audiobin;
    GstElement *equalizer;
    GstElement *preamp;
    gint equalizer_status;
    gdouble current_volume;
    
    // Pipeline/Playback State
    GMutex *mutex;
    GstState target_state;
    guint iterate_timeout_id;
    gboolean buffering;
    gchar *cdda_device;
    
    // Video State
    #ifdef GDK_WINDOWING_X11
    GstXOverlay *xoverlay;
    GdkWindow *video_window;
    #endif
    
    // Clutter State
    #ifdef HAVE_CLUTTER
    GstElement *clutter_sink;
    ClutterTexture *clutter_texture;
    #endif
    
    // Visualization State
    GstElement *vis_resampler;
    GstAdapter *vis_buffer;
    gboolean vis_enabled;
    gboolean vis_thawing;
    GstFFTF32 *vis_fft;
    GstFFTF32Complex *vis_fft_buffer;
    gfloat *vis_fft_sample_buffer;
    
    // Plugin Installer State
    GdkWindow *window;
    GSList *missing_element_details;
    GSList *missing_element_details_handled;
    gboolean handle_missing_elements;
    #ifdef HAVE_GST_PBUTILS
    GstInstallPluginsContext *install_plugins_context;
    #endif
    
    // ReplayGain State
    gboolean replaygain_enabled;
    
    // ReplayGain history: stores the previous 10 scale factors
    // and the current scale factor with the current at index 0
    // and the oldest at index 10. History is used to compute 
    // gain on a track where no adjustment information is present.
    // http://replaygain.hydrogenaudio.org/player_scale.html
    gdouble volume_scale_history[11];
    gboolean volume_scale_history_shift;
    gboolean current_scale_from_history;
    
    // ReplayGain cache
    gdouble album_gain;
    gdouble album_peak;
    gdouble track_gain;
    gdouble track_peak;
};

#endif /* _BANSHEE_PLAYER_PRIVATE_H */
