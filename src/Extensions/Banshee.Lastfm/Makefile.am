ASSEMBLY = Banshee.Lastfm
TARGET = library
LINK = $(REF_EXTENSION_LASTFM) -r:System.Web -r:Mono.Security
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Lastfm.Audioscrobbler/AudioscrobblerService.cs \
	Banshee.Lastfm.Audioscrobbler/Queue.cs \
	Banshee.Lastfm.Radio/LastfmActions.cs \
	Banshee.Lastfm.Radio/LastfmSource.cs \
	Banshee.Lastfm.Radio/LastfmSourceContents.cs \
	Banshee.Lastfm.Radio/LastfmTrackInfo.cs \
	Banshee.Lastfm.Radio/LastfmTrackListModel.cs \
	Banshee.Lastfm.Radio/StationEditor.cs \
	Banshee.Lastfm.Radio/StationSource.cs \
	Banshee.Lastfm.Radio/StationType.cs

RESOURCES =  \
	Banshee.Lastfm.addin.xml \
	Resources/ActiveSourceUI.xml \
	Resources/audioscrobbler.png \
	Resources/AudioscrobblerMenu.xml \
	Resources/GlobalUI.xml \
	Resources/lastfm.glade \
	Resources/neighbour.png \
	Resources/personal.png \
	Resources/recommended.png \
	Resources/tag.png

include $(top_srcdir)/build/build.mk

