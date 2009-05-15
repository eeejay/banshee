ASSEMBLY = Banshee.NowPlaying.Clutter
TARGET = library
LINK = $(REF_EXTENSION_NOWPLAYING_CLUTTER)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.NowPlaying.Clutter/NowPlayingInterface.cs \
	Banshee.NowPlaying.Clutter/NowPlayingSource.cs \
	Clutter.Gtk/Embed.cs \
	Clutter/Actor.cs \
	Clutter/Color.cs \
	Clutter/Group.cs \
	Clutter/Stage.cs \
	Clutter/Texture.cs

RESOURCES = Banshee.NowPlaying.Clutter.addin.xml

#if HAVE_CLUTTER
#include $(top_srcdir)/build/build.mk
#module_SCRIPTS += Banshee.NowPlaying.Clutter.dll.config
#EXTRA_DIST += Banshee.NowPlaying.Clutter.dll.config
#else
EXTRA_DIST = $(SOURCES) $(RESOURCES) Banshee.NowPlaying.Clutter
#endif


