#ifndef _BANSHEE_PLAYER_H
#define _BANSHEE_PLAYER_H

#ifdef HAVE_CONFIG_H
#  include "config.h"
#endif

#include <string.h>
#include <gst/gst.h>
#include <gdk/gdk.h>

#ifdef HAVE_GST_PBUTILS
#  include <gst/pbutils/pbutils.h>
#endif

#ifdef GDK_WINDOWING_X11
#  include <gdk/gdkx.h>
#  include <gst/interfaces/xoverlay.h>
#endif

#define P_INVOKE
#define IS_BANSHEE_PLAYER(e) (e != NULL)
#define SET_CALLBACK(cb_name) { if(player != NULL) { player->cb_name = cb; } }

#define bp_debug g_debug

typedef struct BansheePlayer BansheePlayer;

typedef void (* BansheePlayerEosCallback)          (BansheePlayer *player);
typedef void (* BansheePlayerErrorCallback)        (BansheePlayer *player, GQuark domain, gint code, 
                                                    const gchar *error, const gchar *debug);
typedef void (* BansheePlayerStateChangedCallback) (BansheePlayer *player, GstState old_state, 
                                                    GstState new_state, GstState pending_state);
typedef void (* BansheePlayerIterateCallback)      (BansheePlayer *player);
typedef void (* BansheePlayerBufferingCallback)    (BansheePlayer *player, gint buffering_progress);
typedef void (* BansheePlayerTagFoundCallback)     (BansheePlayer *player, const gchar *tag, const GValue *value);

struct BansheePlayer {

    // Player Callbacks
    BansheePlayerEosCallback eos_cb;
    BansheePlayerErrorCallback error_cb;
    BansheePlayerStateChangedCallback state_changed_cb;
    BansheePlayerIterateCallback iterate_cb;
    BansheePlayerBufferingCallback buffering_cb;
    BansheePlayerTagFoundCallback tag_found_cb;

    // Pipeline Elements
    GstElement *playbin;
    GstElement *audiotee;
    GstElement *audiobin;
    GstElement *equalizer;
    GstElement *preamp;
    
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
    
    // Plugin Installer State
    GdkWindow *window;
    GSList *missing_element_details;
    gboolean install_plugins_noprompt;
    #ifdef HAVE_GST_PBUTILS
    GstInstallPluginsContext *install_plugins_context;
    #endif
};

#endif /* _BANSHEE_PLAYER_H */
