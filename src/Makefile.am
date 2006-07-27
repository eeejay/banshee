SUBDIRS = \
	MusicBrainz \
	Banshee.Widgets \
	Banshee.Base \
	Banshee.MediaEngine \
	Banshee.Dap \
	Banshee.Plugins

MCS_FLAGS = -debug -unsafe -nowarn:0169

TARGET = banshee.exe
BANSHEE_IMPORT_ASSEMBLY = banshee-import.exe

bansheedir = $(pkglibdir)
banshee_SCRIPTS = $(TARGET) $(TARGET).mdb $(TARGET).config $(BANSHEE_IMPORT_ASSEMBLY) $(BANSHEE_IMPORT_ASSEMBLY).mdb
bin_SCRIPTS = banshee banshee-import

EXTRA_DIST = $(banshee_sources) $(banshee_sources_in) \
	$(banshee_import_sources) \
	AssemblyInfo.cs.in \
	AmazonSearchService.wsdl \
	banshee.exe.config.in \
	banshee.in  \
	banshee-import.in \
	BansheeDbusClient.cs \
	BansheeDbusClient.Makefile

CLEANFILES = banshee banshee.exe banshee-import.exe banshee-import *.mdb *.dll
DISTCLEANFILES = entagged-sharp.dll $(banshee_generated_sources)
MAINTAINERCLEANFILES = Makefile.in

banshee_sources_in = AssemblyInfo.cs.in 
banshee_generated_sources = $(banshee_sources_in:.in=) \
	AmazonSearchService.cs

banshee_sources = \
	AboutBox.cs \
	Main.cs \
	StockIcons.cs \
	PlayerInterface.cs \
	PlaylistView.cs \
	Dialogs.cs \
	TrackProperties.cs \
	SourceView.cs \
	PlaylistModel.cs \
	PlaylistColumns.cs \
	IPlaybackModel.cs \
	RemotePlayer.cs \
	RatingRenderer.cs \
	ExceptionDialog.cs \
	VersionInformationDialog.cs \
	LogCoreViewer.cs \
	ToggleStates.cs

banshee_resources = \
	COPYING \
	data/banshee.glade \
	data/images/advanced-icon-32.png \
	data/images/banshee-logo.png \
	data/images/blue-speaker.png \
	data/images/busy-spinner-36.png \
	data/images/busy-spinner.png \
	data/images/circle.png \
	data/images/encoding-icon-32.png \
	data/images/icon-album.png \
	data/images/icon-artist.png \
	data/images/icon-title.png \
	data/images/library-icon-32.png \
	data/images/media-eject.png \
	data/images/media-skip-forward.png \
	data/images/media-playback-pause.png \
	data/images/media-playback-start.png \
	data/images/media-skip-backward.png \
	data/images/media-repeat-all.png \
	data/images/media-repeat-none.png \
	data/images/media-repeat-single.png \
	data/images/media-playlist-shuffle.png \
	data/images/media-playlist-continuous.png \
	data/images/music-player-banshee-16.png \
	data/images/music-player-banshee-22.png \
	data/images/music-player-banshee-24.png \
	data/images/music-player-banshee.png \
	data/images/music-player-banshee.svg \
	data/images/now-playing-arrow.png \
	data/images/playlist-icon-large.png \
	data/images/song-drm.png \
	data/images/source-cd-audio.png \
	data/images/source-library.png \
	data/images/source-playlist.png \
	data/images/source-localqueue.png \
	data/images/star.png \
	data/images/sync-needed.png \
	data/images/tray-icon.png \
	data/images/audio-volume-decrease.png \
	data/images/audio-volume-increase.png \
	data/images/audio-volume-high.png \
	data/images/audio-volume-medium.png \
	data/images/audio-volume-low.png \
	data/images/audio-volume-muted.png \
	data/images/cd-action-rip-16.png \
	data/images/cd-action-rip-24.png \
	data/images/cd-action-burn-24.png \
	data/images/cd-action-burn-32.png \
	data/images/cd-action-burn-48.png \
	data/images/encode-action-24.png

banshee_resources_build = $(foreach res,$(banshee_resources), $(addprefix -resource:,$(srcdir)/../$(res)),$(notdir $(res)))

PKG_REFERENCES = \
	$(GTKSHARP_LIBS) \
	$(DBUSSHARP_LIBS) \
	-r:$(top_builddir)/entagged-sharp/entagged-sharp.dll \
	-r:$(top_builddir)/hal-sharp/hal-sharp.dll \
	-r:$(top_builddir)/burn-sharp/burn-sharp.dll \
	-r:$(top_builddir)/src/MusicBrainz/MusicBrainz.dll \
	-r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll \
	-r:$(top_builddir)/src/Banshee.Widgets/Banshee.Widgets.dll

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

all: AmazonSearchService.cs $(TARGET) $(BANSHEE_IMPORT_ASSEMBLY)

$(TARGET): $(banshee_build_sources) $(top_builddir)/data/banshee.glade
	$(MCS) $(MCS_FLAGS) $(PKG_REFERENCES) $(banshee_resources_build) \
	-target:exe -out:"$@" \
	$(SYSTEM_REFERENCES_BUILD) \
	$(banshee_build_sources)

banshee_import_sources = BansheeImport.cs

$(BANSHEE_IMPORT_ASSEMBLY): $(banshee_import_sources)
	$(MCS) $(MCS_FLAGS) -out:$@ -r:Mono.Posix -r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll -r:$(top_builddir)/entagged-sharp/entagged-sharp.dll $(banshee_import_sources)

AmazonSearchService.cs: AmazonSearchService.wsdl
	MONO_XMLSERIALIZER_THS=no wsdl -namespace:Amazon $(srcdir)/AmazonSearchService.wsdl -o:AmazonSearchService.cs

run:
	sh ./banshee

dbus-client-sample:
	make -f BansheeDbusClient.Makefile

