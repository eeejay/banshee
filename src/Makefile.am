SUBDIRS = \
	GtkSharpBackports \
	MusicBrainz

MCS_FLAGS = -debug -unsafe -nowarn:0169

TARGET = banshee.exe

bansheedir = $(pkglibdir)
banshee_SCRIPTS = $(TARGET) $(TARGET).mdb $(TARGET).config ipod-sharp.dll.config
bin_SCRIPTS = banshee

EXTRA_DIST = $(banshee_sources) $(banshee_sources_in) \
	AssemblyInfo.cs.in \
	ConfigureDefines.cs.in \
	AmazonSearchService.wsdl \
	banshee.exe.config.in \
	ipod-sharp.dll.config.in \
	banshee.in \
	mcs.exe

CLEANFILES = banshee banshee.exe *.mdb *.dll
DISTCLEANFILES = Makefile.in entagged-sharp.dll $(banshee_generated_sources)

banshee_sources_in = AssemblyInfo.cs.in ConfigureDefines.cs.in
banshee_generated_sources = $(banshee_sources_in:.in=) \
	AmazonSearchService.cs

banshee_sources = \
	AboutBox.cs \
	Main.cs \
	StockIcons.cs \
	PlayerInterface.cs \
	Utilities.cs \
	Core.cs \
	PlaylistView.cs \
	VolumeButton.cs \
	Dialogs.cs \
	TrackProperties.cs \
	Database.cs \
	NotificationAreaIcon.cs \
	SqlGenerator.cs \
	Library.cs \
	SignalUtils.cs \
	SourceView.cs \
	Preferences.cs \
	GConfKeys.cs \
	LibraryTransactions.cs \
	LibraryTransactionManager.cs \
	LibraryTransactionStatus.cs \
	LibraryTrackInfo.cs \
	ImageAnimation.cs \
	QueryBuilder.cs \
	QueryBuilderModel.cs \
	DateButton.cs \
	SearchEntry.cs \
	SimpleNotebook.cs \
	TrackInfoHeader.cs \
	DragDrop.cs \
	PlaylistModel.cs \
	PlaylistColumns.cs \
	IPlaybackModel.cs \
	PlaylistRowRenderer.cs \
	DBusIPC.cs \
	IpodCore.cs \
	AlbumCoverFetch.cs \
	FileEncoder.cs \
	GstFileEncoder.cs \
	NautilusBurnUtil.cs \
	FileEncodeTransaction.cs \
	BurnCore.cs \
	BurnTransaction.cs \
	PlayerEngineLoader.cs \
	RatingRenderer.cs \
	IpodTrackInfo.cs \
	AltProgressBar.cs \
	AudioCdCore.cs \
	IpodPropertiesWindow.cs \
	RipTransaction.cs \
	FileNamePattern.cs \
	GstMisc.cs \
	PipelineProfile.cs \
	LogCore.cs \
	EllipsizeLabel.cs \
	IpodNewDialog.cs \
	ExceptionDialog.cs \
	ThemeIcons.cs \
	VersionInformationDialog.cs \
	CoverArtThumbnail.cs \
	AmazonCoverFetcher.cs \
	ActiveUserEventsManager.cs \
	ActiveUserEvent.cs \
	LogCoreViewer.cs \
	FileSystemMonitor/Watcher.cs \
	FileSystemMonitor/Inotify.cs \
	FileSystemMonitor/Watch.cs \
	FileSystemMonitor/InotifyWatch.cs \
	FileSystemMonitor/FileSystemWatcherWatch.cs

banshee_resources = \
	../COPYING \
	../data/Tables.sql \
	../data/glade/player.glade \
	../data/glade/preferences.glade \
	../data/glade/newipod.glade \
	../data/images/tray-icon.png \
	../data/images/media-next.png \
	../data/images/media-pause.png \
	../data/images/media-play.png \
	../data/images/media-prev.png \
	../data/images/volume-decrease.png \
	../data/images/volume-increase.png \
	../data/images/volume-max.png \
	../data/images/volume-med.png \
	../data/images/volume-min.png \
	../data/images/volume-zero.png \
	../data/glade/trackinfo.glade \
	../data/images/icon-album.png \
	../data/images/icon-artist.png \
	../data/images/icon-title.png \
	../data/images/music-player-banshee.png \
	../data/images/media-repeat.png \
	../data/images/media-shuffle.png \
	../data/images/playlist-icon-large.png \
	../data/images/source-library.png \
	../data/images/source-playlist.png \
	../data/images/library-icon-32.png \
	../data/images/busy-spinner.png \
	../data/images/busy-spinner-36.png \
	../data/images/search-entry-icon.png \
	../data/images/search-entry-icon-hover.png \
	../data/images/source-cd-audio.png \
	../data/images/media-eject.png \
	../data/images/blue-speaker.png \
	../data/images/now-playing-arrow.png \
	../data/images/circle.png \
	../data/images/star.png \
	../data/images/advanced-icon-32.png \
	../data/images/encoding-icon-32.png \
	../data/images/sync-needed.png \
	../data/images/song-drm.png \
	../data/images/banshee-logo.png \
	../data/images/cd-action-rip-16.png \
	../data/images/cd-action-rip-24.png \
	../data/images/cd-action-burn-24.png \
	../data/images/cd-action-burn-32.png \
	../data/images/cd-action-burn-48.png \
	../data/images/encode-action-24.png 

banshee_resources_build = $(foreach res,$(banshee_resources), $(addprefix -resource:,$(srcdir)/$(res)),$(notdir $(res)))

PKG_REFERENCES = \
	$(GTKSHARP_LIBS) \
	$(DBUSSHARP_LIBS) \
	$(IPODSHARP_LIBS) \
	-r:$(top_builddir)/entagged-sharp/entagged-sharp.dll \
	-r:$(top_builddir)/hal-sharp/hal-sharp.dll \
	-r:$(top_builddir)/burn-sharp/burn-sharp.dll \
	-r:$(top_builddir)/plugincore/plugin-core.dll \
	-r:$(top_builddir)/src/MusicBrainz/MusicBrainz.dll \
	-r:$(top_builddir)/src/GtkSharpBackports/GtkSharpBackports.dll

SYSTEM_REFERENCES = \
	System.dll \
	System.Xml.dll \
	System.Web.Services.dll \
	System.Data.dll \
	Mono.Data.SqliteClient.dll \
	Mono.Posix 

SYSTEM_REFERENCES_BUILD = $(addprefix -r:, $(SYSTEM_REFERENCES))

banshee_build_sources = $(addprefix $(srcdir)/, $(banshee_sources))
banshee_build_sources += $(banshee_generated_sources)

all: AmazonSearchService.cs $(TARGET)

$(TARGET): $(banshee_build_sources)
	$(MCS) $(MCS_FLAGS) $(PKG_REFERENCES) $(banshee_resources_build) \
	-target:exe -out:"$@" \
	$(SYSTEM_REFERENCES_BUILD) \
	$(banshee_build_sources)

AmazonSearchService.cs: AmazonSearchService.wsdl
	MONO_XMLSERIALIZER_THS=no wsdl -namespace:Amazon $(srcdir)/AmazonSearchService.wsdl -o:AmazonSearchService.cs

install-data-hook:
	for ASM in $(INSTALLED_ASSEMBLIES); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(pkglibdir); \
	done;

uninstall-hook:
	for ASM in $(INSTALLED_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(pkglibdir)/`basename $$ASM`; \
	done;

run:
	BANSHEE_BURN_ENABLE=yes banshee
