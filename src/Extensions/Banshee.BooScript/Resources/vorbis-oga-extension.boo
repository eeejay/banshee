import Banshee.MediaProfiles
import Banshee.ServiceStack

profile_manager = ServiceManager.Get[of MediaProfileManager] ()
vorbis_profile = profile_manager.GetProfileForMimeType ("audio/vorbis")
vorbis_profile.OutputFileExtension = "oga"
