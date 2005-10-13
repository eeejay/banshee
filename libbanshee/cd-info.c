/* ex: set ts=4: */
/***************************************************************************
*  cd-info.c
*
*  Copyright (C) 2005 Novell
*  Written by Aaron Bockover (aaron@aaronbock.net)
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
#  include "config.h"
#endif

#include <stdlib.h>
#include <stdio.h>
#include <unistd.h>
#include <string.h>
#include <fcntl.h>
#include <errno.h>
#include <sys/types.h>
#include <sys/ioctl.h>

#ifdef HAVE_LINUX_CDROM_H
#  include <linux/cdrom.h>
#endif

#include <glib.h>
#include <gst/gst.h>

#include "cd-info.h"

#define TOC_OFFSET 150
#define SECTORS_PER_SEC 75
#define TIME_TO_SECTOR(t) ((t) / GST_SECOND * SECTORS_PER_SEC)
#define SECTOR_TO_TIME(s) (((s) / SECTORS_PER_SEC) * GST_SECOND)

#ifdef HAVE_LINUX_CDROM_H
CdTrackInfo *
cd_track_info_new(int number, gint64 start_sector, gint64 end_sector)
{
    CdTrackInfo *track;

    track = g_new0(CdTrackInfo, 1);

    track->number = number;

    track->start_sector = start_sector;
    track->end_sector = end_sector;
    track->msf_minutes = 0;
    track->msf_seconds = 0;
    track->sectors = track->end_sector - track->start_sector;

    track->start_time = SECTOR_TO_TIME(start_sector);
    track->end_time = SECTOR_TO_TIME(end_sector);

    track->duration = (track->end_time - track->start_time) / GST_SECOND;

    track->minutes = track->duration / 60;
    track->seconds = track->duration % 60;

    track->is_data = FALSE;
    track->is_lead_out = FALSE;

    return track;
}

static void
cd_track_info_free(CdTrackInfo *track)
{
    g_free(track);
    track = NULL;
}

inline static int 
msf_to_frames(struct cdrom_msf0 msf)
{
    return msf.frame + (msf.minute * 60 * 75) + (msf.second * 75);
}

static gint 
cddb_sum(gint n)
{
    gint cddb_sum = 0;
    
    while(n > 0) {
        cddb_sum += n % 10;
        n /= 10;
    }
    
    return cddb_sum;
}

static gchar *
calculate_disc_id(int n_tracks, CdTrackInfo **tracks)
{
    gint i, t = 0, n = 0;
    
    for(i = 0; i < n_tracks; i++) {
        n += cddb_sum((tracks[i]->msf_minutes * 60) +
            tracks[i]->msf_seconds);
    }
    
    t = ((tracks[n_tracks]->msf_minutes * 60) + 
        tracks[n_tracks]->msf_seconds) - 
        ((tracks[0]->msf_minutes * 60) + 
        tracks[0]->msf_seconds);

    return g_strdup_printf("%08x",
        (n % 0xFF) << 24 | t << 8 | n_tracks);
}

static CdTrackInfo * 
cdrom_read_toc_entry_msf(int fd, int track_count, int track_num)
{
    CdTrackInfo *track;
    struct cdrom_tocentry tocentry;
    struct cdrom_tocentry tocentry_next;
    gint64 start_sector = 0, end_sector = 0;
    
    tocentry.cdte_track = track_num;
    tocentry.cdte_format = CDROM_MSF;
        
    if(ioctl(fd, CDROMREADTOCENTRY, &tocentry) < 0) {
        return NULL;
    }        

    tocentry_next.cdte_track = 
        track_num < track_count || track_num == CDROM_LEADOUT ?
        track_num + 1 : CDROM_LEADOUT;
    tocentry_next.cdte_format = CDROM_MSF;
        
    start_sector = msf_to_frames(tocentry.cdte_addr.msf);
        
    if(ioctl(fd, CDROMREADTOCENTRY, &tocentry_next) >= 0) {
        end_sector = msf_to_frames(tocentry_next.cdte_addr.msf);
    }
   
    track = cd_track_info_new(track_num - 1, start_sector, end_sector);
    track->is_data = tocentry.cdte_ctrl == CDROM_DATA_TRACK;
    track->is_lead_out = track_num == CDROM_LEADOUT;
    track->msf_minutes = tocentry.cdte_addr.msf.minute;
    track->msf_seconds = tocentry.cdte_addr.msf.second;
    
    return track;
}

char *
cd_info_get_extended_disc_id(char *device)
{
    int devfd, i, n_tracks;
    struct cdrom_tochdr tochdr;
    CdTrackInfo **tracks;
    gchar *offsets_c;

    GString *offsets = NULL;
    gchar *offset;
 
    devfd = open(device, O_RDONLY | O_NONBLOCK);
    if(devfd == -1) {
        return NULL;
    }   
 
    if(ioctl(devfd, CDROMREADTOCHDR, &tochdr) == -1) {
        close(devfd);
        return NULL;
    }
    
    n_tracks = tochdr.cdth_trk1;
    
    tracks = (CdTrackInfo **)g_new0(CdTrackInfo, n_tracks + 1);
    offsets = g_string_new(NULL);
    
    for(i = tochdr.cdth_trk0; i <= tochdr.cdth_trk1; i++) {
        tracks[i - 1] = cdrom_read_toc_entry_msf(devfd, n_tracks, i);
        
        offset = g_strdup_printf("%" G_GINT64_FORMAT " ", 
            tracks[i - 1]->start_sector);
        g_string_append(offsets, offset);
        g_free(offset);
    }
    
    tracks[n_tracks] = cdrom_read_toc_entry_msf(devfd, 
        n_tracks, CDROM_LEADOUT);
    
    close(devfd);
    
    offsets_c = g_strdup_printf("%s %s", calculate_disc_id(n_tracks, tracks), 
        offsets->str);
    
    g_string_free(offsets, TRUE);
    
    for(i = 0; i < n_tracks; i++) {
        cd_track_info_free(tracks[i]);
    }
    
    g_free(tracks);
    tracks = NULL;
    g_printf("SEC: %lld\n", GST_SECOND);
    return offsets_c;
}
#else
cd_info_get_extended_disc_id(char *device)
{
    GstElement *source;
    GstPad *source_pad;
    char *disc_id;

    gst_init(NULL, NULL);

    source = gst_element_factory_make("cddasrc", "cddasrc");

    if(source == NULL) {
        return NULL;
    }
    
    g_object_set(G_OBJECT(source), "device", device, NULL);
    gst_element_set_state(source, GST_STATE_PLAYING);
    g_object_get(source, "extended-discid", &disc_id, NULL);
    gst_element_set_state(source, GST_STATE_NULL);
    gst_object_unref(GST_OBJECT(source));
    
    return disc_id;
}
#endif
