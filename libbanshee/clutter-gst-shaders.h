
#ifndef CLUTTER_GST_SHADERS_H
#define CLUTTER_GST_SHADERS_H

#include <clutter/clutter.h>

/* Copied from test-shaders */

/* These variables are used instead of the standard GLSL variables on
   GLES 2 */
#ifdef HAVE_COGL_GLES2

#define GLES2_VARS              \
  "precision mediump float;\n"  \
  "varying vec2 tex_coord;\n"   \
  "varying vec4 frag_color;\n"
#define TEX_COORD "tex_coord"
#define COLOR_VAR "frag_color"

#else /* HAVE_COGL_GLES2 */

#define GLES2_VARS ""
#define TEX_COORD "gl_TexCoord[0]"
#define COLOR_VAR "gl_Color"

#endif /* HAVE_COGL_GLES2 */

/* a couple of boilerplate defines that are common amongst all the
 * sample shaders
 */

#define FRAGMENT_SHADER_VARS      \
  GLES2_VARS

/* FRAGMENT_SHADER_END: apply the changed color to the output buffer correctly
 * blended with the gl specified color (makes the opacity of actors work
 * correctly).
 */
#define FRAGMENT_SHADER_END                             \
     "  gl_FragColor = gl_FragColor * " COLOR_VAR ";"

#endif

