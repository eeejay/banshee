ASSEMBLY = Banshee.Daap
TARGET = library
LINK = $(REF_EXTENSION_DAAP)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Daap/DaapContainerSource.cs \
	Banshee.Daap/DaapLoginDialog.cs \
	Banshee.Daap/DaapPlaylistSource.cs \
	Banshee.Daap/DaapProxyWebServer.cs \
	Banshee.Daap/DaapService.cs \
	Banshee.Daap/DaapSource.cs \
	Banshee.Daap/DaapTrackInfo.cs \
	Daap/AuthenticationException.cs \
	Daap/BrokenMD5.cs \
	Daap/Client.cs \
	Daap/ContentCodeBag.cs \
	Daap/ContentFetcher.cs \
	Daap/ContentParser.cs \
	Daap/ContentWriter.cs \
	Daap/Database.cs \
	Daap/Hasher.cs \
	Daap/LoginException.cs \
	Daap/Playlist.cs \
	Daap/ServerInfo.cs \
	Daap/ServiceLocator.cs \
	Daap/Track.cs \
	Daap/User.cs \
	Daap/Utility.cs

RESOURCES =  \
	Banshee.Daap.addin.xml \
	Daap/content-codes

if DAAP_ENABLED
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

