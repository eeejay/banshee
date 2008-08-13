ASSEMBLY = Banshee.Torrent
TARGET = library
LINK = $(LINK_BANSHEE_SERVICES_DEPS) $(LINK_MIGO_DEPS) $(LINK_MONOTORRENT_DBUS)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Torrent/TorrentService.cs \
	Banshee.Torrent/TorrentFileDownloadTask.cs

if HAVE_MONOTORRENT_DBUS
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

