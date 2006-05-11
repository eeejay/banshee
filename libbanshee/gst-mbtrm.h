/***************************************************************************
 *  gst-mbtrm.h
 *
 *  Copyright (C) 2005-2006 Novell, Inc.
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
 
#ifndef __GST_MBTRM_H
#define __GST_MBTRM_H

#include <gst/gst.h>
#include <gst/base/gstbasetransform.h>

#include <musicbrainz/mb_c.h>

GType gst_mbtrm_get_type();

#define GST_TYPE_MBTRM          (gst_mbtrm_get_type())
#define GST_MBTRM(obj)          (G_TYPE_CHECK_INSTANCE_CAST((obj), GST_TYPE_MBTRM, GstMBTrm))
#define GST_MBTRM_CLASS(klass)  (G_TYPE_CHECK_CLASS_CAST((klass), GST_TYPE_MBTRM, GstMBTrm))
#define GST_IS_MBTRM(obj)       (G_TYPE_CHECK_INSTANCE_TYPE((obj), GST_TYPE_MBTRM))
#define GST_IS_MBTRM_CLASS(obj) (G_TYPE_CHECK_CLASS_TYPE((klass), GST_TYPE_MBTRM))

typedef struct _GstMBTrm GstMBTrm;
typedef struct _GstMBTrmClass GstMBTrmClass;

struct _GstMBTrm {
    GstBaseTransform element;
    
    trm_t *trm;
    gboolean trm_done;
    gboolean trm_started;
    
    gchar signature_raw[17];
    gchar signature_ascii[37];
    
    gchar *proxy_address;
    gshort proxy_port;
};

struct _GstMBTrmClass {
    GstBaseTransformClass parent_class;
    void (*have_trm_id)(GstElement *element, const gchar *trm_id);
};

#endif /* __GST_MBTRM_H */
