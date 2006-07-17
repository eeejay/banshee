MCS_FLAGS = -debug 
ASSEMBLY_NAME = Banshee.Base
ASSEMBLY = $(ASSEMBLY_NAME).dll

bansheebasedir = $(pkglibdir)
bansheebase_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

bansheebase_sources_in = ConfigureDefines.cs.in
bansheebase_generated_sources = $(bansheebase_sources_in:.in=)

BANSHEE_CDROM_SOURCES = \
	$(srcdir)/Banshee.Cdrom/IDriveFactory.cs \
	$(srcdir)/Banshee.Cdrom/IDrive.cs \
	$(srcdir)/Banshee.Cdrom/IRecorder.cs \
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
	$(srcdir)/Banshee.Cdrom.Nautilus/NautilusRecorder.cs

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

IO_SOURCES = \
	$(srcdir)/IO/IOProxy.cs \
	$(srcdir)/IO/Interfaces.cs \
	$(srcdir)/IO/SystemIO.cs \
	$(srcdir)/IO/Unix.cs

GUI_SOURCES = \
	$(srcdir)/Gui/GladeDialog.cs \
	$(srcdir)/Gui/ImportDialog.cs \
	$(srcdir)/Gui/OpenLocationDialog.cs \
	$(srcdir)/Gui/UIManager.cs \
	$(srcdir)/Gui/SeekDialog.cs \
	$(srcdir)/Gui/ErrorListDialog.cs \
	$(srcdir)/Gui/SplashScreen.cs \
	$(srcdir)/Gui/PreferencesDialog.cs \
	$(srcdir)/Gui/ArtistAlbumTileView.cs

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
	$(srcdir)/Sources/LibrarySource.cs \
	$(srcdir)/Sources/PlaylistSource.cs \
	$(srcdir)/Sources/AudioCdSource.cs \
	$(srcdir)/Sources/DapSource.cs \
	$(srcdir)/Sources/LocalQueueSource.cs

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
	$(srcdir)/PowerManagement.cs 

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(srcdir)/../../data/UIManagerLayout.xml
	$(MCS) $(MCS_FLAGS) -target:library -r:System.Data -r:Mono.Data.SqliteClient -r:$(top_builddir)/entagged-sharp/entagged-sharp.dll -r:$(top_builddir)/src/Banshee.Widgets/Banshee.Widgets.dll -r:$(top_builddir)/burn-sharp/burn-sharp.dll -r:$(top_builddir)/src/MusicBrainz/MusicBrainz.dll -r:$(top_builddir)/hal-sharp/hal-sharp.dll $(GTKSHARP_LIBS) $(DBUSSHARP_LIBS) -r:Mono.Posix $(DAAPSHARP_LIBS) -resource:$(srcdir)/../../data/UIManagerLayout.xml,UIManagerLayout.xml -resource:$(srcdir)/../../data/images/splash.png,splash.png -out:$@ $(ASSEMBLY_SOURCES) $(bansheebase_generated_sources)

EXTRA_DIST = $(ASSEMBLY_SOURCES) $(bansheebase_sources_in) $(ASSEMBLY).config.in 
CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb
MAINTAINERCLEANFILES = Makefile.in

