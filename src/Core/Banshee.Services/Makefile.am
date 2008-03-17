ASSEMBLY = Banshee.Services
TARGET = library
LINK = $(REF_BANSHEE_SERVICES)

SOURCES =  \
	Banshee.AudioProfiles/Pipeline.cs \
	Banshee.AudioProfiles/PipelineVariable.cs \
	Banshee.AudioProfiles/Profile.cs \
	Banshee.AudioProfiles/ProfileConfiguration.cs \
	Banshee.AudioProfiles/ProfileManager.cs \
	Banshee.Base/RateLimiter.cs \
	Banshee.Base/ThreadAssist.cs \
	Banshee.Collection.Database/AlbumListDatabaseModel.cs \
	Banshee.Collection.Database/ArtistListDatabaseModel.cs \
	Banshee.Collection.Database/DatabaseAlbumInfo.cs \
	Banshee.Collection.Database/DatabaseTrackInfo.cs \
	Banshee.Collection.Database/LibraryArtistInfo.cs \
	Banshee.Collection.Database/TrackListDatabaseModel.cs \
	Banshee.Collection/AlbumListModel.cs \
	Banshee.Collection/ArtistListModel.cs \
	Banshee.Collection/BansheeListModel.cs \
	Banshee.Collection/ExportableModel.cs \
	Banshee.Collection/IExportableModel.cs \
	Banshee.Collection/IHasTrackSelection.cs \
	Banshee.Collection/ImportEventHandler.cs \
	Banshee.Collection/ImportManager.cs \
	Banshee.Collection/ModelHelper.cs \
	Banshee.Collection/SelectAllSelection.cs \
	Banshee.Collection/TrackListModel.cs \
	Banshee.Configuration/DatabaseConfigurationClient.cs \
	Banshee.Database/BansheeDbConnection.cs \
	Banshee.Database/BansheeDbFormatMigrator.cs \
	Banshee.Database/BansheeModelCache.cs \
	Banshee.Database/BansheeModelProvider.cs \
	Banshee.Equalizer/EqualizerManager.cs \
	Banshee.Equalizer/EqualizerSetting.cs \
	Banshee.Equalizer/EqualizerSettingEvent.cs \
	Banshee.Library/HomeDirectoryImportSource.cs \
	Banshee.Library/IImportSource.cs \
	Banshee.Library/ImportSourceManager.cs \
	Banshee.Library/LibraryImportManager.cs \
	Banshee.Library/LibrarySource.cs \
	Banshee.Library/ThreadPoolImportSource.cs \
	Banshee.MediaEngine/IEqualizer.cs \
	Banshee.MediaEngine/IPlayerEngineService.cs \
	Banshee.MediaEngine/NullPlayerEngine.cs \
	Banshee.MediaEngine/PlayerEngine.cs \
	Banshee.MediaEngine/PlayerEngineEvent.cs \
	Banshee.MediaEngine/PlayerEngineService.cs \
	Banshee.MediaEngine/PlayerEngineState.cs \
	Banshee.Metadata.Embedded/EmbeddedMetadataProvider.cs \
	Banshee.Metadata.Embedded/EmbeddedQueryJob.cs \
	Banshee.Metadata.MusicBrainz/MusicBrainzMetadataProvider.cs \
	Banshee.Metadata.MusicBrainz/MusicBrainzQueryJob.cs \
	Banshee.Metadata.Rhapsody/RhapsodyMetadataProvider.cs \
	Banshee.Metadata.Rhapsody/RhapsodyQueryJob.cs \
	Banshee.Metadata/BaseMetadataProvider.cs \
	Banshee.Metadata/IMetadataLookupJob.cs \
	Banshee.Metadata/IMetadataProvider.cs \
	Banshee.Metadata/MetadataService.cs \
	Banshee.Metadata/MetadataServiceJob.cs \
	Banshee.Metadata/MetadataSettings.cs \
	Banshee.Networking/NetworkDetect.cs \
	Banshee.Networking/NetworkManager.cs \
	Banshee.PlaybackController/IBasicPlaybackController.cs \
	Banshee.PlaybackController/ICanonicalPlaybackController.cs \
	Banshee.PlaybackController/IPlaybackController.cs \
	Banshee.PlaybackController/IPlaybackControllerExportable.cs \
	Banshee.PlaybackController/PlaybackControllerDatabaseStack.cs \
	Banshee.PlaybackController/PlaybackControllerService.cs \
	Banshee.PlaybackController/PlaybackRepeatMode.cs \
	Banshee.PlaybackController/PlaybackShuffleMode.cs \
	Banshee.PlayerMigration/AmarokPlayerImportSource.cs \
	Banshee.Playlist/AbstractPlaylistSource.cs \
	Banshee.Playlist/PlaylistFileUtil.cs \
	Banshee.Playlist/PlaylistSource.cs \
	Banshee.Playlists.Formats/AsxPlaylistFormat.cs \
	Banshee.Playlists.Formats/InvalidPlaylistException.cs \
	Banshee.Playlists.Formats/IPlaylistFormat.cs \
	Banshee.Playlists.Formats/M3uPlaylistFormat.cs \
	Banshee.Playlists.Formats/PlaylistFormatBase.cs \
	Banshee.Playlists.Formats/PlaylistFormatDescription.cs \
	Banshee.Playlists.Formats/PlaylistParser.cs \
	Banshee.Playlists.Formats/PlsPlaylistFormat.cs \
	Banshee.Query/AbstractPlaylistQueryValue.cs \
	Banshee.Query/BansheeQuery.cs \
	Banshee.Query/NaturalIntegerQueryValue.cs \
	Banshee.Query/PlaylistQueryValue.cs \
	Banshee.Query/RatingQueryValue.cs \
	Banshee.Query/SmartPlaylistQueryValue.cs \
	Banshee.Query/YearQueryValue.cs \
	Banshee.ServiceStack/Application.cs \
	Banshee.ServiceStack/DBusServiceManager.cs \
	Banshee.ServiceStack/IDBusExportable.cs \
	Banshee.ServiceStack/IExtensionService.cs \
	Banshee.ServiceStack/IInitializeService.cs \
	Banshee.ServiceStack/IService.cs \
	Banshee.ServiceStack/IUserJob.cs \
	Banshee.ServiceStack/ServiceManager.cs \
	Banshee.ServiceStack/ServiceStartedHandler.cs \
	Banshee.ServiceStack/TestUserJob.cs \
	Banshee.ServiceStack/UserJob.cs \
	Banshee.ServiceStack/UserJobEventHandler.cs \
	Banshee.ServiceStack/UserJobManager.cs \
	Banshee.SmartPlaylist/Migrator.cs \
	Banshee.SmartPlaylist/SmartPlaylistCore.cs \
	Banshee.SmartPlaylist/SmartPlaylistSource.cs \
	Banshee.Sources/DatabaseSource.cs \
	Banshee.Sources/ErrorSource.cs \
	Banshee.Sources/IDurationAggregator.cs \
	Banshee.Sources/IFileSizeAggregator.cs \
	Banshee.Sources/IImportable.cs \
	Banshee.Sources/ISource.cs \
	Banshee.Sources/ISourceManager.cs \
	Banshee.Sources/ITrackModelSource.cs \
	Banshee.Sources/IUnmapableSource.cs \
	Banshee.Sources/MessageAction.cs \
	Banshee.Sources/PrimarySource.cs \
	Banshee.Sources/Source.cs \
	Banshee.Sources/SourceManager.cs \
	Banshee.Sources/SourceMergeType.cs \
	Banshee.Sources/SourceMessage.cs \
	Banshee.Streaming/RadioTrackInfo.cs \
	Banshee.Web/Browser.cs

RESOURCES =  \
	Banshee.Services.addin.xml \
	Banshee.Services.addins

module_DATA = Banshee.Services.addins

include $(top_srcdir)/build/build.mk

