SUBDIRS = \
	MusicBrainz \
	Gnome.Keyring \
	Last.FM \
	Banshee.Widgets \
	Banshee.Base \
	Banshee.MediaEngine \
	Banshee.Dap \
	Banshee.Plugins \
	Banshee.Hyena

MCS_FLAGS = -debug -unsafe -nowarn:0169

BANSHEE = banshee
BANSHEE_ASSEMBLY = $(BANSHEE).exe

bansheedir = $(pkglibdir)
bin_SCRIPTS = $(BANSHEE)
banshee_SCRIPTS = \
	$(BANSHEE_ASSEMBLY) \
	$(BANSHEE_ASSEMBLY).mdb \
	$(BANSHEE_ASSEMBLY).config 

BANSHEE_SOURCE_IN = AssemblyInfo.cs.in
BANSHEE_GENERATED_SOURCE = $(BANSHEE_SOURCE_IN:.in=)
	
BANSHEE_SOURCE = \
	$(BANSHEE_GENERATED_SOURCE) \
	Main.cs \
	StockIcons.cs \
	PlayerInterface.cs \
	PlaylistView.cs \
	PlaylistModel.cs \
	PlaylistColumns.cs

BANSHEE_RESOURCES = \
	data/banshee.glade \
	data/images/advanced-icon-32.png \
	data/images/blue-speaker.png \
	data/images/busy-spinner-36.png \
	data/images/busy-spinner.png \
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

BANSHEE_EXPANDED_RESOURCES = $(foreach resource,$(BANSHEE_RESOURCES), $(top_srcdir)/$(resource))

BANSHEE_EXPANDED_RESOURCES_BUILD = $(foreach resource,$(BANSHEE_EXPANDED_RESOURCES), $(addprefix -resource:,$(resource)),$(notdir $(resource)))

BANSHEE_SOURCE_BUILD = $(addprefix $(srcdir)/, $(BANSHEE_SOURCE))

BANSHEE_REFERENCES = \
	$(GTKSHARP_LIBS) \
	$(DBUSSHARP_LIBS) \
	-r:System.Xml \
	-r:System.Web.Services \
	-r:System.Data \
	-r:Mono.Data.SqliteClient \
	-r:Mono.Posix \
	-r:$(top_builddir)/hal-sharp/hal-sharp.dll \
	-r:$(top_builddir)/src/MusicBrainz/MusicBrainz.dll \
	-r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll \
	-r:$(top_builddir)/src/Banshee.Widgets/Banshee.Widgets.dll

all: $(BANSHEE_ASSEMBLY)

$(BANSHEE_ASSEMBLY): $(BANSHEE_SOURCE_BUILD) $(BANSHEE_EXPANDED_RESOURCES)
	$(MCS) -out:$@ $(MCS_FLAGS) $(BANSHEE_REFERENCES) $(BANSHEE_EXPANDED_RESOURCES_BUILD) $(BANSHEE_SOURCE_BUILD)

run: $(BANSHEE_ASSEMBLY)
	sh ./banshee --uninstalled

EXTRA_DIST = \
	$(BANSHEE_SOURCE) \
	$(BANSHEE_SOURCE_IN) \
	$(BANSHEE).in \
	$(BANSHEE_ASSEMBLY).config.in

CLEANFILES = $(BANSHEE) $(BANSHEE_ASSEMBLY) *.mdb *.dll *.pidb
DISTCLEANFILES = $(BANSHEE_GENERATED_SOURCE)
MAINTAINERCLEANFILES = Makefile.in

