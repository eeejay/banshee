
#include <nautilus-burn-recorder.h>

struct NautilusBurnRecorderTrack *
nautilus_burn_glue_create_track (const char *filename,
				 NautilusBurnRecorderTrackType type)
{
	struct NautilusBurnRecorderTrack *track;

	track = g_new0 (typeof (struct NautilusBurnRecorderTrack), 1);

	track->type = type;
	
	if (type == NAUTILUS_BURN_RECORDER_TRACK_TYPE_AUDIO) {
		track->contents.audio.filename = g_strdup (filename);
	} else {
		track->contents.data.filename = g_strdup (filename);
	}

	return track;
}

char *
nautilus_burn_glue_drive_get_id (NautilusBurnDrive *drive)
{
	return g_strdup (drive->cdrecord_id);
}

NautilusBurnDriveType
nautilus_burn_glue_drive_get_type (NautilusBurnDrive *drive)
{
	return drive->type;
}

char *
nautilus_burn_glue_drive_get_display_name (NautilusBurnDrive *drive)
{
	return g_strdup (drive->display_name);
}

int
nautilus_burn_glue_drive_get_max_read_speed (NautilusBurnDrive *drive)
{
	return drive->max_speed_read;
}

int
nautilus_burn_glue_drive_get_max_write_speed (NautilusBurnDrive *drive)
{
	return drive->max_speed_write;
}

char *
nautilus_burn_glue_drive_get_device (NautilusBurnDrive *drive)
{
	return g_strdup (drive->device);
}
