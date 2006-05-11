/***************************************************************************
 *  gst-mbtrm.c
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
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
 
#ifdef HAVE_CONFIG_H
#include "config.h"
#endif

#include <string.h>

#include "gst-mbtrm.h"

GST_DEBUG_CATEGORY(mbtrm_debug);

static GstElementDetails mbtrm_details = {
    "TRM Calculator",
    "Filter/Converter/Audio", // what should this be?
    "Calculate acoustic fingerprint of stream",
    "Aaron Bockover <aaron@abock.org>",
};

enum {
    SIGNAL_HAVE_TRM_ID,
    LAST_SIGNAL
};

enum {
    PROP_0,
    PROP_PROXY_ADDRESS,
    PROP_PROXY_PORT
};

static void gst_mbtrm_dispose(GObject *obj);

static gboolean gst_mbtrm_set_caps(GstBaseTransform *base, 
    GstCaps *incaps, GstCaps *outcaps);
static GstFlowReturn gst_mbtrm_transform_ip(GstBaseTransform *base,
    GstBuffer *buffer);
static gboolean gst_mbtrm_event(GstBaseTransform *base, GstEvent *event);

static void gst_mbtrm_set_property(GObject *object, guint prop_id,
    const GValue *value, GParamSpec *pspec);
static void gst_mbtrm_get_property (GObject *object, guint prop_id,
    GValue * value, GParamSpec * pspec);

static guint gst_mbtrm_signals[LAST_SIGNAL];

#define DEBUG_INIT(bla) \
    GST_DEBUG_CATEGORY_INIT(mbtrm_debug, "mbtrm", 0, "acoustic fingerprint element");

GST_BOILERPLATE_FULL(GstMBTrm, gst_mbtrm, GstBaseTransform, 
    GST_TYPE_BASE_TRANSFORM, DEBUG_INIT);

#define STATIC_CAPS GST_STATIC_CAPS( \
    "audio/x-raw-int, " \
        "rate = (int) [ 1, MAX ], " \
        "channels = (int) [ 1, MAX ], " \
        "endianness = (int) LITTLE_ENDIAN, " \
        "width = (int) { 8, 16 }, " \
        "depth = (int) { 8, 16 }, " "signed = (boolean) true" \
)

static GstStaticPadTemplate gst_mbtrm_src_template =
GST_STATIC_PAD_TEMPLATE("src",
    GST_PAD_SRC,
    GST_PAD_ALWAYS,
    STATIC_CAPS);

static GstStaticPadTemplate gst_mbtrm_sink_template =
GST_STATIC_PAD_TEMPLATE("sink",
    GST_PAD_SINK,
    GST_PAD_ALWAYS,
    STATIC_CAPS);

static void
gst_mbtrm_base_init(gpointer klass)
{
    GstElementClass *element_class = GST_ELEMENT_CLASS(klass);

    gst_element_class_add_pad_template(element_class, 
        gst_static_pad_template_get(&gst_mbtrm_src_template));
    gst_element_class_add_pad_template(element_class,
        gst_static_pad_template_get(&gst_mbtrm_sink_template));
    gst_element_class_set_details(element_class, &mbtrm_details);
}

static void
gst_mbtrm_class_init(GstMBTrmClass *klass)
{
    GObjectClass *gobject_class = G_OBJECT_CLASS(klass);
    
    gobject_class->dispose = gst_mbtrm_dispose;
    
    GST_BASE_TRANSFORM_CLASS(klass)->set_caps =
        GST_DEBUG_FUNCPTR(gst_mbtrm_set_caps);
    GST_BASE_TRANSFORM_CLASS(klass)->transform_ip =
        GST_DEBUG_FUNCPTR(gst_mbtrm_transform_ip);
    GST_BASE_TRANSFORM_CLASS(klass)->event = 
        GST_DEBUG_FUNCPTR(gst_mbtrm_event);

    GST_BASE_TRANSFORM_CLASS(klass)->passthrough_on_same_caps = TRUE;
    
    gobject_class->set_property = gst_mbtrm_set_property;
    gobject_class->get_property = gst_mbtrm_get_property;
  
    g_object_class_install_property(gobject_class, PROP_PROXY_ADDRESS,
        g_param_spec_string("proxy-address", "Proxy Server Address", 
            "Proxy server address that should be used for querying TRM server",
            NULL, G_PARAM_READWRITE));
    
    g_object_class_install_property(gobject_class, PROP_PROXY_PORT,
        g_param_spec_int("proxy-port", "Proxy Server Port", 
            "Proxy server port that should be used for querying TRM server",
            0, G_MAXSHORT, 0, G_PARAM_READWRITE));
        
    gst_mbtrm_signals[SIGNAL_HAVE_TRM_ID] =
        g_signal_new("have-trm-id", G_TYPE_FROM_CLASS(klass),
            G_SIGNAL_RUN_CLEANUP, G_STRUCT_OFFSET(GstMBTrmClass, have_trm_id),
            NULL, NULL,
            gst_marshal_VOID__POINTER, G_TYPE_NONE, 1, G_TYPE_STRING);
}

static void
gst_mbtrm_init(GstMBTrm *this, GstMBTrmClass *klass)
{
    this->trm = NULL;
    this->trm_done = FALSE;
    this->proxy_address = NULL;
    this->proxy_port = 0;
}

static void
gst_mbtrm_set_property(GObject *object, guint prop_id,
    const GValue *value, GParamSpec *pspec)
{
    GstMBTrm *this = GST_MBTRM(object);
    
    switch(prop_id) {
        case PROP_PROXY_ADDRESS: {
            const gchar *temp = g_value_get_string(value);
            
            if(this->proxy_address != NULL) {
                g_free(this->proxy_address);
                this->proxy_address = NULL;
            }
            
            if(temp != NULL) {
                this->proxy_address = g_strdup(temp);
            }
            
            break;
        } 
        case PROP_PROXY_PORT:
            this->proxy_port = g_value_get_int(value);
            break;
        default:
            G_OBJECT_WARN_INVALID_PROPERTY_ID(object, prop_id, pspec);
            break;
    }
}

static void 
gst_mbtrm_get_property(GObject *object, guint prop_id, 
    GValue *value, GParamSpec *pspec)
{
    GstMBTrm *this = GST_MBTRM(object);
    
    switch(prop_id) {
        case PROP_PROXY_ADDRESS:
            g_value_set_string(value, this->proxy_address);
            break;
        case PROP_PROXY_PORT:
            g_value_set_int(value, this->proxy_port);
            break;
        default:
            G_OBJECT_WARN_INVALID_PROPERTY_ID(object, prop_id, pspec);
            break;
    }
}

static void
gst_mbtrm_reset(GstMBTrm *this)
{
    if(this->trm == NULL) {
        return;
    }
    
    trm_Delete(this->trm);
    this->trm = NULL;
}

static void
gst_mbtrm_dispose(GObject *object)
{
    GstMBTrm *this = GST_MBTRM(object);
    
    gst_mbtrm_reset(this);
    
    if(this->proxy_address != NULL) {
        g_free(this->proxy_address);
        this->proxy_address = NULL;
    }
    
    G_OBJECT_CLASS(parent_class)->dispose(object);
}

static gboolean
gst_mbtrm_set_caps(GstBaseTransform *base, GstCaps *incaps,
    GstCaps *outcaps)
{
    GstMBTrm *this = GST_MBTRM(base);
    GstStructure *structure;
    gint channels, rate, bits;
  
    structure = gst_caps_get_structure(incaps, 0);
    gst_structure_get_int(structure, "rate", (gint *)&rate);
    gst_structure_get_int(structure, "channels", (gint *)&channels);
    gst_structure_get_int(structure, "depth", (gint *)&bits);
    
    trm_SetPCMDataInfo(this->trm, rate, channels, bits);
    
    outcaps = incaps;
    
    return TRUE;
}

static GstFlowReturn
gst_mbtrm_transform_ip(GstBaseTransform *base, GstBuffer *buffer)
{
    GstMBTrm *this = GST_MBTRM(base);
    guint8 *buffer_data = GST_BUFFER_DATA(buffer);
    gint buffer_size = GST_BUFFER_SIZE(buffer);
    
    if(this->trm_done) {
        return GST_FLOW_OK;
    }
    
    if(!this->trm_started) {
        gint64 length;
        GstFormat format = GST_FORMAT_TIME;
        GstPad *peer = gst_pad_get_peer(GST_BASE_TRANSFORM_SINK_PAD(base));
        
        if(gst_pad_query_duration(peer, &format, &length)) {
            trm_SetSongLength(this->trm, length / GST_SECOND);
        }
    }
    
    trm_GenerateSignature(this->trm, (gchar *)buffer_data, buffer_size);
    this->trm_started = TRUE;
    
    return GST_FLOW_OK;
}

static void
gst_mbtrm_calculate_trm(GstMBTrm *this)
{
    if(this->trm_done) {
        return;
    }

    memset(this->signature_raw, '\0', sizeof(this->signature_raw));
    memset(this->signature_ascii, '\0', sizeof(this->signature_ascii));
        
    if(!trm_FinalizeSignature(this->trm, this->signature_raw, NULL)) {
        trm_ConvertSigToASCII(this->trm, this->signature_raw, this->signature_ascii);
        g_signal_emit_by_name(this, "have-trm-id", this->signature_ascii, NULL);
    }
        
    this->trm_done = TRUE;
    this->trm_started = FALSE;
}

static gboolean
gst_mbtrm_event(GstBaseTransform *base, GstEvent *event)
{
    GstMBTrm *this = GST_MBTRM(base);
    
    switch(GST_EVENT_TYPE(event)) {
        case GST_EVENT_EOS:
            gst_mbtrm_calculate_trm(this);
            gst_mbtrm_reset(this);
            break;
        case GST_EVENT_NEWSEGMENT:
            gst_mbtrm_reset(this);
            this->trm = trm_New();
            this->trm_started = FALSE;
            
            if(this->proxy_address != NULL) {
                trm_SetProxy(this->trm, this->proxy_address, (gshort)this->proxy_port);
            }
            
            break;
        default:
            break;
    }
    
    return TRUE;
}
