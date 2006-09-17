#include <nautilus-burn-recorder.h>

#ifndef NAUTILUS_BURN_CHECK_VERSION
#define NAUTILUS_BURN_CHECK_VERSION(a,b,c) FALSE
#endif

#define LNB_215 NAUTILUS_BURN_CHECK_VERSION(2,15,3)

#if LNB_215
#include <nautilus-burn.h>
#endif

struct NautilusBurnRecorderTrack *
nautilus_burn_glue_create_track(const char *filename, NautilusBurnRecorderTrackType type)
{
    struct NautilusBurnRecorderTrack *track;

    track = g_new0 (typeof(struct NautilusBurnRecorderTrack), 1);

    track->type = type;
    
    if(type == NAUTILUS_BURN_RECORDER_TRACK_TYPE_AUDIO) {
        track->contents.audio.filename = g_strdup(filename);
    } else {
        track->contents.data.filename = g_strdup(filename);
    }

    return track;
}

char *
nautilus_burn_glue_drive_get_id(NautilusBurnDrive *drive)
{
#if LNB_215
	return g_strdup(nautilus_burn_drive_get_device(drive));
#else
	return g_strdup(drive->cdrecord_id);
#endif 
}

NautilusBurnDriveType
nautilus_burn_glue_drive_get_type(NautilusBurnDrive *drive)
{
#if LNB_215
	return nautilus_burn_drive_get_drive_type(drive);
#else
	return drive->type;
#endif
}

char *
nautilus_burn_glue_drive_get_display_name(NautilusBurnDrive *drive)
{
#if LNB_215
	return nautilus_burn_drive_get_name_for_display (drive);
#else
	return g_strdup(drive->display_name);
#endif
}

int
nautilus_burn_glue_drive_get_max_read_speed(NautilusBurnDrive *drive)
{
#if LNB_215
	return  NAUTILUS_BURN_DRIVE_CD_SPEED(nautilus_burn_drive_get_max_speed_read(drive));
#else 
	return drive->max_speed_read;
#endif
}

int
nautilus_burn_glue_drive_get_max_write_speed(NautilusBurnDrive *drive)
{
#if LNB_215
	return  NAUTILUS_BURN_DRIVE_CD_SPEED(nautilus_burn_drive_get_max_speed_write(drive));
#else 
	return drive->max_speed_write;
#endif
}


char *
nautilus_burn_glue_drive_get_device(NautilusBurnDrive *drive)
{
#if LNB_215
	return g_strdup (nautilus_burn_drive_get_device (drive));
#else
	return g_strdup (drive->device);
#endif
}

gint64
nautilus_glue_burn_drive_get_media_capacity(NautilusBurnDrive *drive)
{
#if LNB_215
	return nautilus_burn_drive_get_media_capacity(drive);
#else
	return nautilus_burn_drive_get_media_size(drive);
#endif
}

NautilusBurnDrive *
nautilus_glue_burn_drive_get_for_device(const char *path)
{
#if LNB_215
	NautilusBurnDriveMonitor *monitor;
	NautilusBurnDrive *drive;
	
	monitor = nautilus_burn_get_drive_monitor();
	drive = nautilus_burn_drive_monitor_get_drive_for_device(monitor, path);
	g_object_unref(monitor);
	
	return drive;
#else

#endif
}

