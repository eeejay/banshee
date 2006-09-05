MCS_FLAGS = -debug 
ASSEMBLY_NAME = Banshee.Base
ASSEMBLY = $(ASSEMBLY_NAME).dll

bansheebasedir = $(pkglibdir)
bansheebase_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

Banshee_Base_SOURCES_IN = ConfigureDefines.cs.in
Banshee_Base_GENERATED_SOURCES = $(Banshee_Base_SOURCES_IN:.in=)

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

IO_SOURCES = \
	$(srcdir)/IO/IOProxy.cs \
	$(srcdir)/IO/Interfaces.cs \
	$(srcdir)/IO/SystemIO.cs \
	$(srcdir)/IO/Unix.cs

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
	$(srcdir)/Gui/ImageFileChooserDialog.cs 

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
	$(srcdir)/Sources/IImportable.cs \
	$(srcdir)/Sources/LibrarySource.cs \
	$(srcdir)/Sources/PlaylistSource.cs \
	$(srcdir)/Sources/AudioCdSource.cs \
	$(srcdir)/Sources/DapSource.cs \
	$(srcdir)/Sources/LocalQueueSource.cs \
	$(srcdir)/Sources/ImportErrorsSource.cs

NETWORKMANAGER_SOURCES = \
	$(srcdir)/NetworkManager/Manager.cs \
	$(srcdir)/NetworkManager/Device.cs \
	$(srcdir)/NetworkManager/Network.cs 

PLUGINS_SOURCES = \
	$(srcdir)/Plugins/Plugin.cs \
	$(srcdir)/Plugins/PluginCore.cs \
	$(srcdir)/Plugins/PluginDialog.cs \
	$(srcdir)/Plugins/IPlugin.cs \
	$(srcdir)/Plugins/PluginFactory.cs

MEDIAENGINE_SOURCES = \
	$(srcdir)/MediaEngine/PlayerEngine.cs \
	$(srcdir)/MediaEngine/IEqualizer.cs

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
	$(BANSHEE_CDROM_ISO_SOURCES) \
	$(BANSHEE_CDROM_ISO_MKISOFS_SOURCES) \
	$(BANSHEE_BURNER_SOURCES) \
	$(BANSHEE_EQUALIZER_SOURCES) \
	$(BANSHEE_EQUALIZER_GUI_SOURCES) \
	$(BANSHEE_GUI_DRAGDROP_SOURCES) \
	$(BANSHEE_KERNEL_SOURCES) \
	$(BANSHEE_LIBRARY_SOURCES) \
	$(BANSHEE_SMARTPLAYLIST_SOURCES) \
	$(Banshee_Base_GENERATED_SOURCES) \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/ActionManager.cs \
	$(srcdir)/HalCore.cs \
	$(srcdir)/Utilities.cs \
	$(srcdir)/ArgumentQueue.cs \
	$(srcdir)/LogCore.cs \
	$(srcdir)/AmazonCoverFetcher.cs \
	$(srcdir)/Paths.cs \
	$(srcdir)/TrackInfo.cs \
	$(srcdir)/FileNamePattern.cs \
	$(srcdir)/Globals.cs \
	$(srcdir)/GConfKeys.cs \
	$(srcdir)/SpecialKeys.cs \
	$(srcdir)/BansheeTodo.cs \
	$(srcdir)/IconThemeUtils.cs \
	$(srcdir)/GstMisc.cs \
	$(srcdir)/ImportManager.cs \
	$(srcdir)/UriList.cs \
	$(srcdir)/PipelineProfile.cs \
	$(srcdir)/Transcoder.cs \
	$(srcdir)/GstTranscoder.cs \
	$(srcdir)/BatchTranscoder.cs \
	$(srcdir)/NetworkDetect.cs \
	$(srcdir)/PlayerEngineCore.cs \
	$(srcdir)/TrackInfoHeader.cs \
	$(srcdir)/TrackInfoPopup.cs \
	$(srcdir)/Library.cs \
	$(srcdir)/Database.cs \
	$(srcdir)/SqlGenerator.cs \
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
	$(srcdir)/ProductInformation.cs

Banshee_Base_RESOURCES = \
	data/UIManagerLayout.xml \
	data/contributors.xml \
	data/banshee-dialogs.glade \
	data/images/splash.png \
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
	$(MCS) $(MCS_FLAGS) -target:library -r:System.Data -r:Mono.Data.SqliteClient -r:$(top_srcdir)/taglib-sharp/taglib-sharp.dll -r:$(top_builddir)/src/Banshee.Widgets/Banshee.Widgets.dll -r:$(top_builddir)/burn-sharp/burn-sharp.dll -r:$(top_builddir)/src/MusicBrainz/MusicBrainz.dll -r:$(top_builddir)/hal-sharp/hal-sharp.dll $(GTKSHARP_LIBS) $(DBUSSHARP_LIBS) -r:Mono.Posix $(DAAPSHARP_LIBS) -out:$@ $(Banshee_Base_EXPANDED_RESOURCES_BUILD) $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) $(bansheebase_sources_in) $(ASSEMBLY).config.in 
CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb
MAINTAINERCLEANFILES = Makefile.in

