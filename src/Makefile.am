# CS0169 is for methods declared that are not used... since all methods that
# that cause this warning are assigned at runtime to an event handler set
# in glade, the warning is disabled... it's extremely annoying!

MCS_FLAGS = -debug -nowarn:0169

sonancewrapdir = $(bindir)
sonancewrap_SCRIPTS = sonance

sonancedir = $(pkglibdir)
sonance_SCRIPTS = sonance.exe sonance.exe.config gst-sharp.dll

EXTRA_DIST = $(sonance_sources) $(sonance_sources_in) \
	gst-sharp.dll \
	sonance.in \
	AssemblyInfo.cs.in \
	AmazonSearchService.wsdl

CLEANFILES = sonance.exe sonance Makefile.Sonance Sonance.mdp *.mdb
DISTCLEANFILES = Makefile.in sonance entagged-sharp.dll $(sonance_generated_sources)

sonance_sources_in = AssemblyInfo.cs.in
sonance_generated_sources = $(sonance_sources_in:.in=) \
	AmazonSearchService.cs

sonance_sources = \
	AboutBox.cs \
	DebugLog.cs \
	Main.cs \
	StockIcons.cs \
	PlayerInterface.cs \
	TrackInfo.cs \
	GstPlayer.cs \
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
	DecoderRegistry.cs \
	ImageAnimation.cs \
	QueryBuilder.cs \
	QueryBuilderModel.cs \
	DateButton.cs \
	SearchEntry.cs \
	SimpleNotebook.cs \
	TrackInfoHeader.cs \
	DragDrop.cs \
	PlaylistModel.cs \
	InstanceFinder.cs \
	PlaylistColumns.cs \
	IPlaybackModel.cs \
	PlaylistRowRenderer.cs \
	DBusIPC.cs \
	PlayerEngine.cs \
	IpodCore.cs

sonance_resources = \
	../data/Tables.sql \
	../data/glade/player.glade \
	../data/glade/preferences.glade \
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
	../data/glade/about.glade \
	../data/glade/trackinfo.glade \
	../data/images/sonance-splash.png \
	../data/images/album-cover-container.png \
	../data/images/icon-album.png \
	../data/images/icon-artist.png \
	../data/images/icon-title.png \
	../data/images/sonance-icon.png \
	../data/images/media-repeat.png \
	../data/images/media-shuffle.png \
	../data/images/playlist-icon-large.png \
	../data/images/source-library-icon.png \
	../data/images/source-playlist-icon.png \
	../data/images/library-icon.png \
	../data/images/library-icon-32.png \
	../data/images/busy-spinner.png \
	../data/images/busy-spinner-36.png \
	../data/images/sonance-icon-24.png \
	../data/images/search-entry-icon.png \
	../data/images/search-entry-icon-hover.png
	
sonance_resources_build = $(foreach res,$(sonance_resources), $(addprefix -resource:,$(srcdir)/$(res)),$(notdir $(res)))

PKG_REFERENCES = \
	$(GTKSHARP_LIBS) \
	$(DBUSSHARP_LIBS) \
	$(IPODSHARP_LIBS) \
	-r:$(top_builddir)/entagged/entagged-sharp.dll \
	-r:$(srcdir)/gst-sharp.dll

SYSTEM_REFERENCES = \
	System.dll \
	System.Xml.dll \
	System.Web.Services.dll \
	System.Data.dll \
	Mono.Data.SqliteClient.dll \
	Mono.Posix

SYSTEM_REFERENCES_BUILD = $(addprefix -r:, $(SYSTEM_REFERENCES))

sonance_build_sources = $(addprefix $(srcdir)/, $(sonance_sources))
sonance_build_sources += $(sonance_generated_sources)

all: AmazonSearchService.cs sonance.exe

sonance.exe: $(sonance_build_sources)
	$(MCS) $(MCS_FLAGS) $(PKG_REFERENCES) $(sonance_resources_build) \
	-target:exe -out:"$@" \
	$(SYSTEM_REFERENCES_BUILD) \
	$(sonance_build_sources)

AmazonSearchService.cs: AmazonSearchService.wsdl
	MONO_XMLSERIALIZER_THS=no wsdl -namespace:Amazon $(srcdir)/AmazonSearchService.wsdl -o:AmazonSearchService.cs

install-data-hook:
	for asm in `$(PKG_CONFIG) --variable=Libraries ipod-sharp`; do \
		$(INSTALL) -c -m 0755 $$asm $(pkglibdir); \
	done;

run:
	mono --debug --config /usr/lib/sonance/sonance.exe.config sonance.exe
