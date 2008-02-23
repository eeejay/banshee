ASSEMBLY = Banshee.Lastfm
TARGET = library
LINK = $(REF_EXTENSION_LASTFM) -r:System.Web -r:Mono.Security

SOURCES =  \
    Banshee.Lastfm.Audioscrobbler/AudioscrobblerService.cs \
    Banshee.Lastfm.Audioscrobbler/Queue.cs \
	Banshee.Lastfm.Radio/Connection.cs \
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
	Resources/AudioscrobblerMenu.xml \
	Resources/audioscrobbler.png \
	Resources/GlobalUI.xml \
	Resources/lastfm.glade \
	Resources/recommended.png

include $(top_srcdir)/build/build.mk

