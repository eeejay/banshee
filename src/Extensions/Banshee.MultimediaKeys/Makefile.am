ASSEMBLY = Banshee.MultimediaKeys
TARGET = library
LINK = $(REF_EXTENSION_MULTIMEDIAKEYS)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = Banshee.MultimediaKeys/MultimediaKeysService.cs 

RESOURCES = Resources/Banshee.MultimediaKeys.addin.xml

if ENABLE_GNOME
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
