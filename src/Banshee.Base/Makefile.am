MCS_FLAGS = -debug 
ASSEMBLY_NAME = Banshee.Base
ASSEMBLY = $(ASSEMBLY_NAME).dll

bansheebasedir = $(pkglibdir)
bansheebase_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

bansheebase_sources_in = ConfigureDefines.cs.in
bansheebase_generated_sources = $(bansheebase_sources_in:.in=)

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
	$(srcdir)/Plugins/PluginDialog.cs

ASSEMBLY_SOURCES = \
    $(AUDIOCD_SOURCES) \
    $(DAP_SOURCES) \
    $(SOURCES_SOURCES) \
    $(NETWORKMANAGER_SOURCES) \
    $(PLUGINS_SOURCES) \
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
	$(srcdir)/IPlayerEngine.cs \
	$(srcdir)/PlayerEngineLoader.cs \
	$(srcdir)/SpecialKeys.cs \
	$(srcdir)/BansheeTodo.cs \
	$(srcdir)/IconThemeUtils.cs \
	$(srcdir)/GstMisc.cs \
	$(srcdir)/ImportManager.cs \
	$(srcdir)/UriList.cs \
	$(srcdir)/PipelineProfile.cs \
	$(srcdir)/FileEncoder.cs \
	$(srcdir)/GstFileEncoder.cs \
	$(srcdir)/FileEncodeAction.cs \
	$(srcdir)/NetworkDetect.cs \
	$(srcdir)/PlayerEngineCore.cs \
	$(srcdir)/TrackInfoHeader.cs \
	$(srcdir)/TrackInfoPopup.cs \
	$(srcdir)/Library.cs \
	$(srcdir)/Database.cs \
	$(srcdir)/SqlGenerator.cs \
	$(srcdir)/LibraryTrackInfo.cs \
	$(srcdir)/FileTrackInfo.cs \
	$(srcdir)/DBusRemote.cs

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(srcdir)/../../data/UIManagerLayout.xml
	$(MCS) $(MCS_FLAGS) -target:library -r:System.Data -r:Mono.Data.SqliteClient -r:$(top_builddir)/entagged-sharp/entagged-sharp.dll -r:$(top_builddir)/src/Banshee.Widgets/Banshee.Widgets.dll -r:$(top_builddir)/src/MusicBrainz/MusicBrainz.dll -r:$(top_builddir)/hal-sharp/hal-sharp.dll $(GTKSHARP_LIBS) $(DBUSSHARP_LIBS) -r:Mono.Posix $(DAAPSHARP_LIBS) -resource:$(srcdir)/../../data/UIManagerLayout.xml,UIManagerLayout.xml -resource:$(srcdir)/../../data/Tables.sql,Tables.sql -out:$@ $(ASSEMBLY_SOURCES) $(bansheebase_generated_sources)

EXTRA_DIST = $(ASSEMBLY_SOURCES) $(bansheebase_sources_in) $(ASSEMBLY).config.in 
CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb Makefile.in 

