include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = Banshee.Base
ASSEMBLY = $(ASSEMBLY_NAME).dll
ASSEMBLY_GCONF_SCHEMA = banshee-core.schemas.in

bansheebasedir = $(pkglibdir)
bansheebase_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

Banshee_Base_SOURCES_IN = ConfigureDefines.cs.in
Banshee_Base_GENERATED_SOURCES = $(Banshee_Base_SOURCES_IN:.in=)

BANSHEE_AUDIOPROFILES_SOURCES = \
	$(srcdir)/Banshee.AudioProfiles/ProfileManager.cs \
	$(srcdir)/Banshee.AudioProfiles/Profile.cs \
	$(srcdir)/Banshee.AudioProfiles/Pipeline.cs \
	$(srcdir)/Banshee.AudioProfiles/PipelineVariable.cs \
	$(srcdir)/Banshee.AudioProfiles/ProfileConfiguration.cs \
	$(srcdir)/Banshee.AudioProfiles/GConfProfileConfiguration.cs \
	$(srcdir)/Banshee.AudioProfiles/Abakos.Compiler/*.cs

BANSHEE_AUDIOPROFILES_GUI_SOURCES = \
	$(srcdir)/Banshee.AudioProfiles.Gui/ProfileConfigurationDialog.cs \
	$(srcdir)/Banshee.AudioProfiles.Gui/ProfileComboBox.cs \
	$(srcdir)/Banshee.AudioProfiles.Gui/ProfileComboBoxConfigurable.cs \
	$(srcdir)/Banshee.AudioProfiles.Gui/ProfileConfigureButton.cs

BANSHEE_CDROM_SOURCES = \
	$(srcdir)/Banshee.Cdrom/IDriveFactory.cs \
	$(srcdir)/Banshee.Cdrom/IDrive.cs \
	$(srcdir)/Banshee.Cdrom/IRecorder.cs \
	$(srcdir)/Banshee.Cdrom/IDiscDuplicator.cs \
	$(srcdir)/Banshee.Cdrom/DriveMediaType.cs \
	$(srcdir)/Banshee.Cdrom/RecorderAction.cs \
	$(srcdir)/Banshee.Cdrom/RecorderTrack.cs \
	$(srcdir)/Banshee.Cdrom/RecorderResult.cs

BANSHEE_CDROM_GUI_SOURCES = \
	$(srcdir)/Banshee.Cdrom.Gui/DriveComboBox.cs \
	$(srcdir)/Banshee.Cdrom.Gui/RecorderSpeedComboBox.cs

BANSHEE_CDROM_NAUTILUS_SOURCES = \
	$(srcdir)/Banshee.Cdrom.Nautilus/NautilusDriveFactory.cs \
	$(srcdir)/Banshee.Cdrom.Nautilus/NautilusDrive.cs \
	$(srcdir)/Banshee.Cdrom.Nautilus/NautilusRecorder.cs \
	$(srcdir)/Banshee.Cdrom.Nautilus/NautilusDiscDuplicator.cs 

BANSHEE_CDROM_NAUTILUS_INTEROP_SOURCES = \
	$(srcdir)/Banshee.Cdrom.Nautilus.Interop/BurnDrive.cs \
	$(srcdir)/Banshee.Cdrom.Nautilus.Interop/BurnRecorder.cs \
	$(srcdir)/Banshee.Cdrom.Nautilus.Interop/BurnRecorderTrack.cs \
	$(srcdir)/Banshee.Cdrom.Nautilus.Interop/BurnRecorderHandlers.cs \
	$(srcdir)/Banshee.Cdrom.Nautilus.Interop/NautilusEnums.cs

BANSHEE_CDROM_ISO_SOURCES = \
	$(srcdir)/Banshee.Cdrom.Iso/IImageCreator.cs \
	$(srcdir)/Banshee.Cdrom.Iso/ImageStatus.cs

BANSHEE_CDROM_ISO_MKISOFS_SOURCES = \
	$(srcdir)/Banshee.Cdrom.Iso.Mkisofs/MkisofsProcess.cs \
	$(srcdir)/Banshee.Cdrom.Iso.Mkisofs/MkisofsImageCreator.cs

BANSHEE_BURNER_SOURCES = \
	$(srcdir)/Banshee.Burner/BurnerCore.cs \
	$(srcdir)/Banshee.Burner/BurnerSource.cs \
	$(srcdir)/Banshee.Burner/BurnerOptionsDialog.cs \
	$(srcdir)/Banshee.Burner/BurnerConfigurationPane.cs \
	$(srcdir)/Banshee.Burner/BurnerFormatList.cs \
	$(srcdir)/Banshee.Burner/BurnerUtilities.cs \
	$(srcdir)/Banshee.Burner/BurnerSession.cs \
	$(srcdir)/Banshee.Burner/BurnerSessionPreparer.cs \
	$(srcdir)/Banshee.Burner/BurnerSessionRecorder.cs

BANSHEE_EQUALIZER_SOURCES = \
	$(srcdir)/Banshee.Equalizer/EqualizerManager.cs \
	$(srcdir)/Banshee.Equalizer/EqualizerSetting.cs

BANSHEE_EQUALIZER_GUI_SOURCES = \
	$(srcdir)/Banshee.Equalizer.Gui/EqualizerView.cs \
	$(srcdir)/Banshee.Equalizer.Gui/EqualizerWindow.cs \
	$(srcdir)/Banshee.Equalizer.Gui/EqualizerBandScale.cs \
	$(srcdir)/Banshee.Equalizer.Gui/EqualizerLevelsBox.cs \
	$(srcdir)/Banshee.Equalizer.Gui/EqualizerPresetComboBox.cs

BANSHEE_GUI_DRAGDROP_SOURCES = \
	$(srcdir)/Banshee.Gui.DragDrop/DragDropList.cs \
	$(srcdir)/Banshee.Gui.DragDrop/DragDropTarget.cs \
	$(srcdir)/Banshee.Gui.DragDrop/DragDropUtilities.cs 

BANSHEE_KERNEL_SOURCES = \
	$(srcdir)/Banshee.Kernel/IntervalHeap.cs \
	$(srcdir)/Banshee.Kernel/IJob.cs \
	$(srcdir)/Banshee.Kernel/IInstanceCriticalJob.cs \
	$(srcdir)/Banshee.Kernel/Job.cs \
	$(srcdir)/Banshee.Kernel/JobPriority.cs \
	$(srcdir)/Banshee.Kernel/Scheduler.cs

BANSHEE_LIBRARY_SOURCES = \
	$(srcdir)/Banshee.Library/Import.cs \
	$(srcdir)/Banshee.Library/DatabaseWriteException.cs

BANSHEE_SMARTPLAYLIST_SOURCES = \
	$(srcdir)/Banshee.SmartPlaylist/Editor.cs \
	$(srcdir)/Banshee.SmartPlaylist/QueryBuilder.cs \
	$(srcdir)/Banshee.SmartPlaylist/QueryBuilderModel.cs \
	$(srcdir)/Banshee.SmartPlaylist/SmartPlaylistCore.cs \
	$(srcdir)/Banshee.SmartPlaylist/SmartPlaylistSource.cs

BANSHEE_PLAYLISTS_FORMATS_SOURCES = \
    $(srcdir)/Banshee.Playlists.Formats/PlaylistFile.cs \
    $(srcdir)/Banshee.Playlists.Formats/M3u.cs \
    $(srcdir)/Banshee.Playlists.Formats/Pls.cs  

BANSHEE_PLAYLISTS_FORMATS_XSPF_SOURCES = \
	$(srcdir)/Banshee.Playlists.Formats.Xspf/XspfBaseObject.cs \
	$(srcdir)/Banshee.Playlists.Formats.Xspf/Playlist.cs \
	$(srcdir)/Banshee.Playlists.Formats.Xspf/Track.cs \
	$(srcdir)/Banshee.Playlists.Formats.Xspf/MetaEntry.cs \
	$(srcdir)/Banshee.Playlists.Formats.Xspf/LinkEntry.cs \
	$(srcdir)/Banshee.Playlists.Formats.Xspf/XmlUtil.cs \
	$(srcdir)/Banshee.Playlists.Formats.Xspf/W3CDateTime.cs

BANSHEE_WEB_SOURCES = \
	$(srcdir)/Banshee.Web/Browser.cs

BANSHEE_CONFIGURATION_SOURCES = \
	$(srcdir)/Banshee.Configuration/ConfigurationClient.cs \
	$(srcdir)/Banshee.Configuration/GConfConfigurationClient.cs \
	$(srcdir)/Banshee.Configuration/IConfigurationClient.cs \
	$(srcdir)/Banshee.Configuration/SchemaEntry.cs

BANSHEE_CONFIGURATION_SCHEMA_SOURCES = \
	$(srcdir)/Banshee.Configuration.Schema/LibrarySchema.cs \
	$(srcdir)/Banshee.Configuration.Schema/ImportSchema.cs \
	$(srcdir)/Banshee.Configuration.Schema/PlayerWindowSchema.cs

BANSHEE_METADATA_SOURCES = \
	$(srcdir)/Banshee.Metadata/IBasicTrackInfo.cs \
	$(srcdir)/Banshee.Metadata/IMetadataProvider.cs \
	$(srcdir)/Banshee.Metadata/IMetadataLookupJob.cs \
	$(srcdir)/Banshee.Metadata/SchedulerMetadataLookupJob.cs \
	$(srcdir)/Banshee.Metadata/SchedulerMetadataProvider.cs \
	$(srcdir)/Banshee.Metadata/MetadataProviderFactory.cs \
	$(srcdir)/Banshee.Metadata/MultipleMetadataProvider.cs

BANSHEE_METADATA_RHAPSODY_SOURCES = \
	$(srcdir)/Banshee.Metadata.Rhapsody/RhapsodyMetadataProvider.cs \
	$(srcdir)/Banshee.Metadata.Rhapsody/RhapsodyQueryJob.cs

IO_SOURCES = \
	$(srcdir)/IO/Utilities.cs \
	$(srcdir)/IO/IOProxy.cs \
	$(srcdir)/IO/Interfaces.cs \
	$(srcdir)/IO/SystemIO.cs \
	$(srcdir)/IO/Unix.cs \
	$(srcdir)/IO/GnomeVfs.cs

GUI_SOURCES = \
	$(srcdir)/Gui/GladeWindow.cs \
	$(srcdir)/Gui/GladeDialog.cs \
	$(srcdir)/Gui/ImportDialog.cs \
	$(srcdir)/Gui/OpenLocationDialog.cs \
	$(srcdir)/Gui/UIManager.cs \
	$(srcdir)/Gui/SeekDialog.cs \
	$(srcdir)/Gui/ErrorListDialog.cs \
	$(srcdir)/Gui/SplashScreen.cs \
	$(srcdir)/Gui/PreferencesDialog.cs \
	$(srcdir)/Gui/AboutDialog.cs \
	$(srcdir)/Gui/ExceptionDialog.cs \
	$(srcdir)/Gui/CleanRoomStartup.cs \
	$(srcdir)/Gui/LogCoreDialog.cs \
	$(srcdir)/Gui/VersionInformationDialog.cs \
	$(srcdir)/Gui/ToggleStates.cs \
	$(srcdir)/Gui/RatingRenderer.cs \
	$(srcdir)/Gui/TrackEditor.cs \
	$(srcdir)/Gui/ConfirmShutdownDialog.cs \
	$(srcdir)/Gui/ImageFileChooserDialog.cs \
	$(srcdir)/Gui/SourceView.cs \
	$(srcdir)/Gui/FileChooserDialog.cs

AUDIOCD_SOURCES = \
	$(srcdir)/AudioCd/AudioCdCore.cs \
	$(srcdir)/AudioCd/AudioCdTrackInfo.cs \
	$(srcdir)/AudioCd/AudioCdDisk.cs

DAP_SOURCES = \
	$(srcdir)/Dap/Dap.cs \
	$(srcdir)/Dap/DapCore.cs \
	$(srcdir)/Dap/DapTrackInfo.cs \
	$(srcdir)/Dap/DapMisc.cs \
	$(srcdir)/Dap/DapPropertiesDialog.cs 

SOURCES_SOURCES = \
	$(srcdir)/SourceManager.cs \
	$(srcdir)/Source.cs \
	$(srcdir)/ChildSource.cs \
	$(srcdir)/Sources/AbstractPlaylistSource.cs \
	$(srcdir)/Sources/IImportable.cs \
	$(srcdir)/Sources/LibrarySource.cs \
	$(srcdir)/Sources/PlaylistSource.cs \
	$(srcdir)/Sources/AudioCdSource.cs \
	$(srcdir)/Sources/DapSource.cs \
	$(srcdir)/Sources/LocalQueueSource.cs \
	$(srcdir)/Sources/ImportErrorsSource.cs \
	$(srcdir)/Sources/TestSource.cs \
	$(srcdir)/Sources/IPlaylistCapable.cs \
	$(srcdir)/Sources/DapPlaylistSource.cs

NETWORKMANAGER_SOURCES = \
	$(srcdir)/NetworkManager/Manager.cs 

PLUGINS_SOURCES = \
	$(srcdir)/Plugins/Plugin.cs \
	$(srcdir)/Plugins/PluginCore.cs \
	$(srcdir)/Plugins/PluginDialog.cs \
	$(srcdir)/Plugins/IPlugin.cs \
	$(srcdir)/Plugins/PluginFactory.cs

MEDIAENGINE_SOURCES = \
	$(srcdir)/MediaEngine/PlayerEngine.cs \
	$(srcdir)/MediaEngine/IEqualizer.cs

NOTIFICATIONS_SOURCES = \
	$(srcdir)/Notifications/Notification.cs \
	$(srcdir)/Notifications/Notifications.cs

ASSEMBLY_SOURCES = \
    $(AUDIOCD_SOURCES) \
    $(DAP_SOURCES) \
    $(SOURCES_SOURCES) \
    $(NETWORKMANAGER_SOURCES) \
    $(PLUGINS_SOURCES) \
	$(GUI_SOURCES) \
	$(MEDIAENGINE_SOURCES) \
	$(IO_SOURCES) \
	$(BANSHEE_CDROM_SOURCES) \
	$(BANSHEE_CDROM_GUI_SOURCES) \
	$(BANSHEE_CDROM_NAUTILUS_SOURCES) \
	$(BANSHEE_CDROM_NAUTILUS_INTEROP_SOURCES) \
	$(BANSHEE_CDROM_ISO_SOURCES) \
	$(BANSHEE_CDROM_ISO_MKISOFS_SOURCES) \
	$(BANSHEE_BURNER_SOURCES) \
	$(BANSHEE_EQUALIZER_SOURCES) \
	$(BANSHEE_EQUALIZER_GUI_SOURCES) \
	$(BANSHEE_GUI_DRAGDROP_SOURCES) \
	$(BANSHEE_KERNEL_SOURCES) \
	$(BANSHEE_LIBRARY_SOURCES) \
	$(BANSHEE_SMARTPLAYLIST_SOURCES) \
	$(BANSHEE_PLAYLISTS_FORMATS_SOURCES) \
	$(BANSHEE_PLAYLISTS_FORMATS_XSPF_SOURCES) \
	$(BANSHEE_WEB_SOURCES) \
	$(BANSHEE_CONFIGURATION_SOURCES) \
	$(BANSHEE_CONFIGURATION_SCHEMA_SOURCES) \
	$(BANSHEE_METADATA_SOURCES) \
	$(BANSHEE_METADATA_RHAPSODY_SOURCES) \
	$(NOTIFICATIONS_SOURCES) \
	$(BANSHEE_AUDIOPROFILES_SOURCES) \
	$(BANSHEE_AUDIOPROFILES_GUI_SOURCES) \
	$(Banshee_Base_GENERATED_SOURCES) \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/ActionManager.cs \
	$(srcdir)/HalCore.cs \
	$(srcdir)/Utilities.cs \
	$(srcdir)/StringUtil.cs \
	$(srcdir)/ArgumentQueue.cs \
	$(srcdir)/LogCore.cs \
	$(srcdir)/AmazonCoverFetcher.cs \
	$(srcdir)/Paths.cs \
	$(srcdir)/TrackInfo.cs \
	$(srcdir)/FileNamePattern.cs \
	$(srcdir)/Globals.cs \
	$(srcdir)/SpecialKeys.cs \
	$(srcdir)/IconThemeUtils.cs \
	$(srcdir)/GstMisc.cs \
	$(srcdir)/ImportManager.cs \
	$(srcdir)/UriList.cs \
	$(srcdir)/Transcoder.cs \
	$(srcdir)/GstTranscoder.cs \
	$(srcdir)/BatchTranscoder.cs \
	$(srcdir)/NetworkDetect.cs \
	$(srcdir)/PlayerEngineCore.cs \
	$(srcdir)/TrackInfoHeader.cs \
	$(srcdir)/TrackInfoPopup.cs \
	$(srcdir)/Library.cs \
	$(srcdir)/Database.cs \
	$(srcdir)/LibraryTrackInfo.cs \
	$(srcdir)/FileTrackInfo.cs \
	$(srcdir)/DBusRemote.cs \
    $(srcdir)/QueuedOperationManager.cs \
	$(srcdir)/QueuedSqliteDatabase.cs \
	$(srcdir)/AudioCdRipper.cs \
	$(srcdir)/IImportSource.cs \
	$(srcdir)/ImportSources.cs \
	$(srcdir)/FolderImportSource.cs \
	$(srcdir)/FileImportSource.cs \
	$(srcdir)/HomeDirectoryImportSource.cs \
	$(srcdir)/UnknownTrackInfo.cs \
	$(srcdir)/StreamTagger.cs \
	$(srcdir)/GstTagger.cs \
	$(srcdir)/SafeUri.cs \
	$(srcdir)/Catalog.cs \
	$(srcdir)/Branding.cs \
	$(srcdir)/BansheeBranding.cs \
	$(srcdir)/ComponentInitializer.cs \
	$(srcdir)/PowerManagement.cs \
	$(srcdir)/ProductInformation.cs \
	$(srcdir)/DBusPlayer.cs \
	$(srcdir)/GenericCollectionController.cs \
	$(srcdir)/SampleTrackInfo.cs 

Banshee_Base_RESOURCES = \
	data/UIManagerLayout.xml \
	data/contributors.xml \
	data/banshee-dialogs.glade \
	data/images/splash.png \
	data/images/banshee-logo.png \
	data/images/media-repeat-none.png \
	data/images/media-repeat-single.png \
	data/images/media-repeat-all.png \
	data/images/media-playlist-shuffle.png \
	data/images/media-playlist-continuous.png \
	data/images/rating-rated.png \
	data/images/rating-unrated.png \
	data/images/source-smart-playlist.png \
	data/images/editor-cover-album.png \
	COPYING

Banshee_Base_EXPANDED_RESOURCES = $(foreach resource,$(Banshee_Base_RESOURCES), $(top_srcdir)/$(resource))
Banshee_Base_EXPANDED_RESOURCES += $(top_builddir)/data/translators.xml

Banshee_Base_EXPANDED_RESOURCES_BUILD = $(foreach resource,$(Banshee_Base_EXPANDED_RESOURCES), $(addprefix -resource:,$(resource)),$(notdir $(resource)))

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(Banshee_Base_EXPANDED_RESOURCES)
	$(BUILD_LIB) $(LNB_CSFLAGS) -out:$@ $(LINK_TAGLIB) $(LINK_BANSHEE_WIDGETS) $(LINK_MUSICBRAINZ) $(LINK_LAST_FM) $(LINK_HAL) $(LINK_DBUS) $(LINK_GTK) $(LINK_MONO_UNIX) $(LINK_BOO) $(LINK_BOOBUDDY) -r:System.Data -r:Mono.Data.SqliteClient -r:Mono.Cairo $(Banshee_Base_EXPANDED_RESOURCES_BUILD) $(ASSEMBLY_SOURCES)

include $(top_srcdir)/build/gconf-schema-rules

EXTRA_DIST = \
	$(ASSEMBLY_SOURCES) \
	$(bansheebase_sources_in) \
	$(ASSEMBLY).config.in \
	$(ASSEMBLY_GCONF_SCHEMA)
	
CLEANFILES = $(ASSEMBLY) *.dll *.exe 
DISTCLEANFILES = *.mdb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in

