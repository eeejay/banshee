/*
 * Copyright (C) 2004 Jorn Baayen <jorn@nl.linux.org>
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

#ifndef __METADATA_H__
#define __METADATA_H__

#include <glib.h>
#include <gdk-pixbuf/gdk-pixbuf.h>

typedef struct _Metadata Metadata;

Metadata   *metadata_load                (const char *filename,
				          char **error_message_return);

void        metadata_free                (Metadata *metadata);

const char *metadata_get_title           (Metadata *metadata);

const char *metadata_get_artist          (Metadata *metadata,
				          int index);
int         metadata_get_artist_count    (Metadata *metadata);

const char *metadata_get_performer       (Metadata *metadata,
				          int index);
int         metadata_get_performer_count (Metadata *metadata);

const char *metadata_get_album           (Metadata *metadata);

GdkPixbuf  *metadata_get_album_art       (Metadata *metadata);

int         metadata_get_track_number    (Metadata *metadata);

int         metadata_get_total_tracks    (Metadata *metadata);

int         metadata_get_disc_number     (Metadata *metadata);

int         metadata_get_duration        (Metadata *metadata);

const char *metadata_get_year            (Metadata *metadata);

const char *metadata_get_mime_type       (Metadata *metadata);

int         metadata_get_mtime           (Metadata *metadata);

double      metadata_get_gain            (Metadata *metadata);
double      metadata_get_peak            (Metadata *metadata);

#endif /* __METADATA_H__ */
