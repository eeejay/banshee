ASSEMBLY = Banshee.Moblin
TARGET = library
LINK = $(REF_EXTENSION_MOBLIN) $(LINK_EXTENSION_PLAYQUEUE_DEPS)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Moblin/MediaPanelContents.cs \
	Banshee.Moblin/MoblinService.cs \
	Banshee.Moblin/MoblinTrackInfoDisplay.cs \
	Banshee.Moblin/PlaybackBox.cs \
	Banshee.Moblin/PlayQueueBox.cs \
	Banshee.Moblin/SearchEntry.cs \
	Banshee.Moblin/SearchHeader.cs \
	Mutter/PanelClient.cs \
	Mutter/PanelGtk.cs

RESOURCES = Banshee.Moblin.addin.xml

if HAVE_MOBLIN
include $(top_srcdir)/build/build.mk
module_SCRIPTS += Banshee.Moblin.dll.config
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

EXTRA_DIST += Banshee.Moblin.dll.config
