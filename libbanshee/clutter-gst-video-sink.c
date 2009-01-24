/*
 * Clutter-GStreamer.
 *
 * GStreamer integration library for Clutter.
 *
 * clutter-gst-video-sink.h - Gstreamer Video Sink that renders to a
 *                            Clutter Texture.
 *
 * Authored by Jonathan Matthew  <jonathan@kaolin.wh9.net>,
 *             Chris Lord        <chris@openedhand.com>
 *
 * Copyright (C) 2007,2008 OpenedHand
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

/**
 * SECTION:clutter-gst-video-sink
 * @short_description: GStreamer video sink
 *
 * #ClutterGstVideoSink is a GStreamer sink element that sends
 * data to a #ClutterTexture.
 */

#include "config.h"

#include "clutter-gst-video-sink.h"
#include "clutter-gst-shaders.h"

#include <gst/gst.h>
#include <gst/gstvalue.h>
#include <gst/video/video.h>
#include <gst/riff/riff-ids.h>

#include <glib.h>
#include <clutter/clutter.h>
#include <string.h>

static gchar *ayuv_to_rgba_shader = \
     FRAGMENT_SHADER_VARS
     "uniform sampler2D tex;"
     "void main () {"
     "  vec4 color = texture2D (tex, vec2(" TEX_COORD "));"
     "  float y = 1.1640625 * (color.g - 0.0625);"
     "  float u = color.b - 0.5;"
     "  float v = color.a - 0.5;"
     "  color.a = color.r;"
     "  color.r = y + 1.59765625 * v;"
     "  color.g = y - 0.390625 * u - 0.8125 * v;"
     "  color.b = y + 2.015625 * u;"
     "  gl_FragColor = color;"
     FRAGMENT_SHADER_END
     "}";

static gchar *dummy_shader = \
     FRAGMENT_SHADER_VARS
     "void main () {"
     "}";

static gchar *yv12_to_rgba_shader = \
     FRAGMENT_SHADER_VARS
     "uniform sampler2D ytex;"
     "uniform sampler2D utex;"
     "uniform sampler2D vtex;"
     "void main () {"
     "  vec2 coord = vec2(" TEX_COORD ");"
     "  float y = 1.1640625 * (texture2D (ytex, coord).g - 0.0625);"
     "  float u = texture2D (utex, coord).g - 0.5;"
     "  float v = texture2D (vtex, coord).g - 0.5;"
     "  vec4 color;"
     "  color.r = y + 1.59765625 * v;"
     "  color.g = y - 0.390625 * u - 0.8125 * v;"
     "  color.b = y + 2.015625 * u;"
     "  color.a = 1.0;"
     "  gl_FragColor = color;"
     FRAGMENT_SHADER_END
     "}";

static GstStaticPadTemplate sinktemplate 
 = GST_STATIC_PAD_TEMPLATE ("sink",
                            GST_PAD_SINK,
                            GST_PAD_ALWAYS,
                            GST_STATIC_CAPS (GST_VIDEO_CAPS_RGBx ";"   \
                                             GST_VIDEO_CAPS_BGRx "; " \
                                             GST_VIDEO_CAPS_RGB ";"   \
                                             GST_VIDEO_CAPS_BGR \
                                             ));

/* Multi-texturing will only be available on GL, so decide on capabilties
 * accordingly.
 */

#ifdef CLUTTER_COGL_HAS_GL
#define YUV_CAPS GST_VIDEO_CAPS_YUV("AYUV") ";" GST_VIDEO_CAPS_YUV("YV12") ";"
#else
#define YUV_CAPS GST_VIDEO_CAPS_YUV("AYUV") ";"
#endif

/* Don't advertise RGB/BGR as it seems to override yv12, even when it's the
 * better choice. Unfortunately, RGBx/BGRx also override AYUV when it's the
 * better choice too, but that's not quite as bad.
 */

static GstStaticPadTemplate sinktemplate_shaders 
 = GST_STATIC_PAD_TEMPLATE ("sink",
                            GST_PAD_SINK,
                            GST_PAD_ALWAYS,
                            GST_STATIC_CAPS (YUV_CAPS \
                                             GST_VIDEO_CAPS_RGBx ";" \
                                             GST_VIDEO_CAPS_BGRx));

static GstStaticPadTemplate sinktemplate_all 
 = GST_STATIC_PAD_TEMPLATE ("sink",
                            GST_PAD_SINK,
                            GST_PAD_ALWAYS,
                            GST_STATIC_CAPS (YUV_CAPS \
                                             GST_VIDEO_CAPS_RGBx ";" \
                                             GST_VIDEO_CAPS_BGRx ";" \
                                             GST_VIDEO_CAPS_RGB ";" \
                                             GST_VIDEO_CAPS_BGR));

GST_DEBUG_CATEGORY_STATIC (clutter_gst_video_sink_debug);
#define GST_CAT_DEFAULT clutter_gst_video_sink_debug

static GstElementDetails clutter_gst_video_sink_details =
  GST_ELEMENT_DETAILS ("Clutter video sink",
      "Sink/Video",
      "Sends video data from a GStreamer pipeline to a Clutter texture",
      "Jonathan Matthew <jonathan@kaolin.wh9.net>, "
      "Matthew Allum <mallum@o-hand.com, "
      "Chris Lord <chris@o-hand.com>");

enum
{
  PROP_0,
  PROP_TEXTURE,
  PROP_USE_SHADERS,
};

typedef enum
{
  CLUTTER_GST_NOFORMAT,
  CLUTTER_GST_RGB32,
  CLUTTER_GST_RGB24,
  CLUTTER_GST_AYUV,
  CLUTTER_GST_YV12,
} ClutterGstVideoFormat;

typedef void (*GLUNIFORM1IPROC)(COGLint location, COGLint value);

struct _ClutterGstVideoSinkPrivate
{
  ClutterTexture        *texture;
  CoglHandle             u_tex;
  CoglHandle             v_tex;
  CoglHandle             program;
  CoglHandle             shader;
  GAsyncQueue           *async_queue;
  ClutterGstVideoFormat  format;
  gboolean               bgr;
  int                    width;
  int                    height;
  int                    fps_n, fps_d;
  int                    par_n, par_d;
  gboolean               use_shaders;
  gboolean               shaders_init;
  
  GLUNIFORM1IPROC        glUniform1iARB;
};


#define _do_init(bla) \
  GST_DEBUG_CATEGORY_INIT (clutter_gst_video_sink_debug, \
                                 "cluttersink", \
                                 0, \
                                 "clutter video sink")

GST_BOILERPLATE_FULL (ClutterGstVideoSink,
                          clutter_gst_video_sink,
                      GstBaseSink,
                      GST_TYPE_BASE_SINK,
                      _do_init);

static void
clutter_gst_video_sink_base_init (gpointer g_class)
{
  GstElementClass *element_class = GST_ELEMENT_CLASS (g_class);

  gst_element_class_add_pad_template 
                     (element_class,
                      gst_static_pad_template_get (&sinktemplate_all));

  gst_element_class_set_details (element_class, 
                                 &clutter_gst_video_sink_details);
}

static void
clutter_gst_video_sink_init (ClutterGstVideoSink      *sink,
                             ClutterGstVideoSinkClass *klass)
{
  ClutterGstVideoSinkPrivate *priv;

  sink->priv = priv =
    G_TYPE_INSTANCE_GET_PRIVATE (sink, CLUTTER_GST_TYPE_VIDEO_SINK,
                                 ClutterGstVideoSinkPrivate);

  priv->async_queue = g_async_queue_new ();

#ifdef CLUTTER_COGL_HAS_GL
  priv->glUniform1iARB = (GLUNIFORM1IPROC)
    cogl_get_proc_address ("glUniform1iARB");
#endif
}

static void
clutter_gst_video_sink_paint (ClutterActor        *actor,
                              ClutterGstVideoSink *sink)
{
  ClutterGstVideoSinkPrivate *priv = sink->priv;
  if (priv->program)
    cogl_program_use (priv->program);
}

static void
clutter_gst_yv12_paint (ClutterActor        *actor,
                        ClutterGstVideoSink *sink)
{
#ifdef CLUTTER_COGL_HAS_GL
  ClutterGstVideoSinkPrivate *priv = sink->priv;
  GLuint texture;
  
  /* Bind the U and V textures in texture units 1 and 2 */
  if (priv->u_tex)
    {
      cogl_texture_get_gl_texture (priv->u_tex, &texture, NULL);
      glActiveTexture (GL_TEXTURE1);
      glEnable (GL_TEXTURE_2D);
      glBindTexture (GL_TEXTURE_2D, texture);
    }

  if (priv->v_tex)
    {
      cogl_texture_get_gl_texture (priv->v_tex, &texture, NULL);
      glActiveTexture (GL_TEXTURE2);
      glEnable (GL_TEXTURE_2D);
      glBindTexture (GL_TEXTURE_2D, texture);
    }
  
  glActiveTexture (GL_TEXTURE0_ARB);
#endif
}

static void
clutter_gst_yv12_post_paint (ClutterActor        *actor,
                             ClutterGstVideoSink *sink)
{
#ifdef CLUTTER_COGL_HAS_GL
  /* Disable the extra texture units */
  glActiveTexture (GL_TEXTURE1);
  glDisable (GL_TEXTURE_2D);
  glActiveTexture (GL_TEXTURE2);
  glDisable (GL_TEXTURE_2D);
  glActiveTexture (GL_TEXTURE0);
#endif
}

static void
clutter_gst_video_sink_set_shader (ClutterGstVideoSink *sink,
                                   const gchar         *shader_src)
{
  ClutterGstVideoSinkPrivate *priv = sink->priv;
  
  priv->shaders_init = FALSE;
  if (priv->texture)
    clutter_actor_set_shader (CLUTTER_ACTOR (priv->texture), NULL);

  if (priv->program)
    {
      cogl_program_unref (priv->program);
      priv->program = NULL;
    }
  
  if (priv->shader)
    {
      cogl_program_unref (priv->shader);
      priv->shader = NULL;
    }
  
  if (shader_src)
    {
      ClutterShader *shader;

      /* Set a dummy shader so we don't interfere with the shader stack */
      shader = clutter_shader_new ();
      clutter_shader_set_fragment_source (shader, dummy_shader, -1);
      clutter_actor_set_shader (CLUTTER_ACTOR (priv->texture), shader);
      g_object_unref (shader);

      /* Create shader through COGL - necessary as we need to be able to set
       * integer uniform variables for multi-texturing.
       */
      priv->shader = cogl_create_shader (CGL_FRAGMENT_SHADER);
      cogl_shader_source (priv->shader, shader_src);
      cogl_shader_compile (priv->shader);
      
      priv->program = cogl_create_program ();
      cogl_program_attach_shader (priv->program, priv->shader);
      cogl_program_link (priv->program);

      /* Hook onto the pre-paint signal to replace the dummy shader with
       * the real shader.
       */
      g_signal_connect (priv->texture,
                        "paint",
                        G_CALLBACK (clutter_gst_video_sink_paint),
                        sink);
      
      priv->shaders_init = TRUE;
    }
}

static gboolean
clutter_gst_video_sink_idle_func (gpointer data)
{
  ClutterGstVideoSink        *sink;
  ClutterGstVideoSinkPrivate *priv;
  GstBuffer                  *buffer;

  sink = data;
  priv = sink->priv;

  buffer = g_async_queue_try_pop (priv->async_queue);
  if (buffer == NULL || G_UNLIKELY (!GST_IS_BUFFER (buffer)))
    {
      return FALSE;
    }

  if ((priv->format == CLUTTER_GST_RGB32) || (priv->format == CLUTTER_GST_AYUV))
    {
      clutter_texture_set_from_rgb_data (priv->texture,
                                         GST_BUFFER_DATA (buffer),
                                         TRUE,
                                         priv->width,
                                         priv->height,
                                         GST_ROUND_UP_4 (4 * priv->width),
                                         4,
                                         priv->bgr ?
                                         CLUTTER_TEXTURE_RGB_FLAG_BGR : 0,
                                         NULL);

      /* Initialise AYUV shader */
      if ((priv->format == CLUTTER_GST_AYUV) && !priv->shaders_init)
        clutter_gst_video_sink_set_shader (sink,
                                           ayuv_to_rgba_shader);
    }
  else if (priv->format == CLUTTER_GST_RGB24)
    {
      clutter_texture_set_from_rgb_data (priv->texture,
                                         GST_BUFFER_DATA (buffer),
                                         FALSE,
                                         priv->width,
                                         priv->height,
                                         GST_ROUND_UP_4 (3 * priv->width),
                                         3,
                                         priv->bgr ?
                                         CLUTTER_TEXTURE_RGB_FLAG_BGR : 0,
                                         NULL);
    }
  else if (priv->format == CLUTTER_GST_YV12)
    {
      CoglHandle y_tex =
        cogl_texture_new_from_data (priv->width,
                                    priv->height,
                                    -1,
                                    FALSE,
                                    COGL_PIXEL_FORMAT_G_8,
                                    COGL_PIXEL_FORMAT_G_8,
                                    priv->width,
                                    GST_BUFFER_DATA (buffer));
      cogl_texture_set_filters (y_tex, CGL_LINEAR, CGL_LINEAR);
      clutter_texture_set_cogl_texture (priv->texture, y_tex);
      cogl_texture_unref (y_tex);
      
      if (priv->u_tex)
        cogl_texture_unref (priv->u_tex);
      
      if (priv->v_tex)
        cogl_texture_unref (priv->v_tex);
      
      priv->v_tex =
        cogl_texture_new_from_data (priv->width/2,
                                    priv->height/2,
                                    -1,
                                    FALSE,
                                    COGL_PIXEL_FORMAT_G_8,
                                    COGL_PIXEL_FORMAT_G_8,
                                    priv->width/2,
                                    GST_BUFFER_DATA (buffer) +
                                      (priv->width * priv->height));
      cogl_texture_set_filters (priv->v_tex, CGL_LINEAR, CGL_LINEAR);

      priv->u_tex =
        cogl_texture_new_from_data (priv->width/2,
                                    priv->height/2,
                                    -1,
                                    FALSE,
                                    COGL_PIXEL_FORMAT_G_8,
                                    COGL_PIXEL_FORMAT_G_8,
                                    priv->width/2,
                                    GST_BUFFER_DATA (buffer) +
                                      (priv->width * priv->height) +
                                      (priv->width/2 * priv->height/2));
      cogl_texture_set_filters (priv->u_tex, CGL_LINEAR, CGL_LINEAR);
      
      /* Initialise YV12 shader */
      if (!priv->shaders_init)
        {
#ifdef CLUTTER_COGL_HAS_GL
          COGLint location;
          clutter_gst_video_sink_set_shader (sink,
                                             yv12_to_rgba_shader);
          
          cogl_program_use (priv->program);
          location = cogl_program_get_uniform_location (priv->program, "ytex");
          priv->glUniform1iARB (location, 0);
          location = cogl_program_get_uniform_location (priv->program, "utex");
          priv->glUniform1iARB (location, 1);
          location = cogl_program_get_uniform_location (priv->program, "vtex");
          priv->glUniform1iARB (location, 2);
          cogl_program_use (COGL_INVALID_HANDLE);
          
          g_signal_connect (priv->texture,
                            "paint",
                            G_CALLBACK (clutter_gst_yv12_paint),
                            sink);
          g_signal_connect_after (priv->texture,
                                  "paint",
                                  G_CALLBACK (clutter_gst_yv12_post_paint),
                                  sink);
#endif
        }
    }

  gst_buffer_unref (buffer);
  
  return FALSE;
}

static GstFlowReturn
clutter_gst_video_sink_render (GstBaseSink *bsink,
                               GstBuffer   *buffer)
{
  ClutterGstVideoSink *sink;
  ClutterGstVideoSinkPrivate *priv;

  sink = CLUTTER_GST_VIDEO_SINK (bsink);
  priv = sink->priv;

  g_async_queue_push (priv->async_queue, gst_buffer_ref (buffer));

  clutter_threads_add_idle_full (G_PRIORITY_HIGH_IDLE,
                                 clutter_gst_video_sink_idle_func,
                                 sink,
                                 NULL);

  return GST_FLOW_OK;
}

static GstCaps *
clutter_gst_video_sink_get_caps (GstBaseSink *sink)
{
  ClutterGstVideoSinkPrivate *priv = CLUTTER_GST_VIDEO_SINK (sink)->priv;
  
  if (priv->use_shaders && cogl_features_available (COGL_FEATURE_SHADERS_GLSL))
    return gst_static_pad_template_get_caps (&sinktemplate_shaders);
  else
    return gst_static_pad_template_get_caps (&sinktemplate);
}

static gboolean
clutter_gst_video_sink_set_caps (GstBaseSink *bsink,
                                 GstCaps     *caps)
{
  ClutterGstVideoSink        *sink;
  ClutterGstVideoSinkPrivate *priv;
  GstCaps                    *intersection;
  GstStructure               *structure;
  gboolean                    ret;
  const GValue               *fps;
  const GValue               *par;
  gint                        width, height;
  guint32                     fourcc;
  int                         red_mask, blue_mask;

  sink = CLUTTER_GST_VIDEO_SINK(bsink);
  priv = sink->priv;

  clutter_gst_video_sink_set_shader (sink, NULL);

  if (priv->use_shaders && cogl_features_available (COGL_FEATURE_SHADERS_GLSL))
    intersection 
      = gst_caps_intersect 
            (gst_static_pad_template_get_caps (&sinktemplate_shaders), 
             caps);
  else
    intersection 
      = gst_caps_intersect 
            (gst_static_pad_template_get_caps (&sinktemplate), 
             caps);

  if (gst_caps_is_empty (intersection)) 
    return FALSE;

  gst_caps_unref (intersection);

  structure = gst_caps_get_structure (caps, 0);

  ret  = gst_structure_get_int (structure, "width", &width);
  ret &= gst_structure_get_int (structure, "height", &height);
  fps  = gst_structure_get_value (structure, "framerate");
  ret &= (fps != NULL);

  par  = gst_structure_get_value (structure, "pixel-aspect-ratio");

  if (!ret)
    return FALSE;

  priv->width  = width;
  priv->height = height;

  /* We dont yet use fps or pixel aspect into but handy to have */
  priv->fps_n  = gst_value_get_fraction_numerator (fps);
  priv->fps_d  = gst_value_get_fraction_denominator (fps);

  if (par) 
    {
      priv->par_n = gst_value_get_fraction_numerator (par);
      priv->par_d = gst_value_get_fraction_denominator (par);
    } 
  else 
    priv->par_n = priv->par_d = 1;

  ret = gst_structure_get_fourcc (structure, "format", &fourcc);
  if (ret && (fourcc == GST_RIFF_YV12))
    {
      priv->format = CLUTTER_GST_YV12;
    }
  else if (ret && (fourcc == GST_MAKE_FOURCC ('A', 'Y', 'U', 'V')))
    {
      priv->format = CLUTTER_GST_AYUV;
      priv->bgr = FALSE;
    }
  else
    {
      guint32 width;
      gst_structure_get_int (structure, "red_mask", &red_mask);
      gst_structure_get_int (structure, "blue_mask", &blue_mask);
      
      width = red_mask | blue_mask;
      if (width < 0x1000000)
        {
          priv->format = CLUTTER_GST_RGB24;
          priv->bgr = (red_mask == 0xff0000) ? FALSE : TRUE;
        }
      else
        {
          priv->format = CLUTTER_GST_RGB32;
          priv->bgr = (red_mask == 0xff000000) ? FALSE : TRUE;
        }
    }

  return TRUE;
}

static void
clutter_gst_video_sink_dispose (GObject *object)
{
  ClutterGstVideoSink *self;
  ClutterGstVideoSinkPrivate *priv;

  self = CLUTTER_GST_VIDEO_SINK (object);
  priv = self->priv;

  clutter_gst_video_sink_set_shader (self, NULL);

  if (priv->texture)
    {
      g_object_unref (priv->texture);
      priv->texture = NULL;
    }

  if (priv->async_queue)
    {
      g_async_queue_unref (priv->async_queue);
      priv->async_queue = NULL;
    }

  G_OBJECT_CLASS (parent_class)->dispose (object);
}

static void
clutter_gst_video_sink_finalize (GObject *object)
{
  ClutterGstVideoSink *self;
  ClutterGstVideoSinkPrivate *priv;

  self = CLUTTER_GST_VIDEO_SINK (object);
  priv = self->priv;

  G_OBJECT_CLASS (parent_class)->finalize (object);
}

static void
clutter_gst_video_sink_set_property (GObject *object,
                                     guint prop_id,
                                     const GValue *value,
                                     GParamSpec *pspec)
{
  ClutterGstVideoSink *sink;
  ClutterGstVideoSinkPrivate *priv;
  gboolean use_shaders;

  sink = CLUTTER_GST_VIDEO_SINK (object);
  priv = sink->priv;

  switch (prop_id) 
    {
    case PROP_TEXTURE:
      if (priv->texture)
        g_object_unref (priv->texture);

      priv->texture = CLUTTER_TEXTURE (g_value_dup_object (value));
      break;
    case PROP_USE_SHADERS:
      use_shaders = g_value_get_boolean (value);
      if (priv->use_shaders != use_shaders)
        {
          priv->use_shaders = use_shaders;
          g_object_notify (object, "use_shaders");
        }
      break;
    default:
      G_OBJECT_WARN_INVALID_PROPERTY_ID (object, prop_id, pspec);
      break;
    }
}

static void
clutter_gst_video_sink_get_property (GObject *object,
                                     guint prop_id,
                                     GValue *value,
                                     GParamSpec *pspec)
{
  ClutterGstVideoSink *sink;

  sink = CLUTTER_GST_VIDEO_SINK (object);

  switch (prop_id) 
    {
    case PROP_TEXTURE:
      g_value_set_object (value, sink->priv->texture);
      break;
    case PROP_USE_SHADERS:
      g_value_set_boolean (value, sink->priv->use_shaders);
      break;
    default:
      G_OBJECT_WARN_INVALID_PROPERTY_ID (object, prop_id, pspec);
      break;
  }
}

static gboolean
clutter_gst_video_sink_stop (GstBaseSink *base_sink)
{
  ClutterGstVideoSinkPrivate *priv;
  GstBuffer *buffer;

  priv = CLUTTER_GST_VIDEO_SINK (base_sink)->priv;

  g_async_queue_lock (priv->async_queue);

  /* Remove all remaining objects from the queue */
  do
    {
      buffer = g_async_queue_try_pop_unlocked (priv->async_queue);
      if (buffer)
        gst_buffer_unref (buffer);
    } while (buffer != NULL);

  g_async_queue_unlock (priv->async_queue);

  return TRUE;
}

static void
clutter_gst_video_sink_class_init (ClutterGstVideoSinkClass *klass)
{
  GObjectClass *gobject_class = G_OBJECT_CLASS (klass);
  GstBaseSinkClass *gstbase_sink_class = GST_BASE_SINK_CLASS (klass);

  g_type_class_add_private (klass, sizeof (ClutterGstVideoSinkPrivate));

  gobject_class->set_property = clutter_gst_video_sink_set_property;
  gobject_class->get_property = clutter_gst_video_sink_get_property;

  gobject_class->dispose = clutter_gst_video_sink_dispose;
  gobject_class->finalize = clutter_gst_video_sink_finalize;

  gstbase_sink_class->render = clutter_gst_video_sink_render;
  gstbase_sink_class->preroll = clutter_gst_video_sink_render;
  gstbase_sink_class->stop = clutter_gst_video_sink_stop;
  gstbase_sink_class->set_caps = clutter_gst_video_sink_set_caps;
  gstbase_sink_class->get_caps = clutter_gst_video_sink_get_caps;

  g_object_class_install_property 
              (gobject_class, PROP_TEXTURE,
               g_param_spec_object ("texture",
                                    "texture",
                                    "Target ClutterTexture object",
                                    CLUTTER_TYPE_TEXTURE,
                                    G_PARAM_READWRITE));

  g_object_class_install_property 
              (gobject_class, PROP_USE_SHADERS,
               g_param_spec_boolean ("use_shaders",
                                     "Use shaders",
                                     "Use a fragment shader to accelerate "
                                     "colour-space conversion.",
                                     FALSE,
                                     G_PARAM_READWRITE));
}

/**
 * clutter_gst_video_sink_new:
 * @texture: a #ClutterTexture
 *
 * Creates a new GStreamer video sink which uses @texture as the target
 * for sinking a video stream from GStreamer.
 *
 * Return value: a #GstElement for the newly created video sink
 */
GstElement *
clutter_gst_video_sink_new (ClutterTexture *texture)
{
  return g_object_new (CLUTTER_GST_TYPE_VIDEO_SINK,
                       "texture", texture,
                       NULL);
}

static gboolean
plugin_init (GstPlugin *plugin)
{
  gboolean ret = gst_element_register (plugin,
                                             "cluttersink",
                                       GST_RANK_PRIMARY,
                                       CLUTTER_GST_TYPE_VIDEO_SINK);
  return ret;
}

GST_PLUGIN_DEFINE_STATIC (GST_VERSION_MAJOR,
                          GST_VERSION_MINOR,
                          "cluttersink",
                          "Element to render to Clutter textures",
                          plugin_init,
                          VERSION,
                          "LGPL", /* license */
                          PACKAGE,
                          "");
