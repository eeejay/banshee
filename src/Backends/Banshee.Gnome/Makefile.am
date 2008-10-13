ASSEMBLY = Banshee.Gnome
TARGET = library
LINK = $(REF_BACKEND_GNOME)
INSTALL_DIR = $(BACKENDS_INSTALL_DIR)

SOURCES =  \
	Banshee.GnomeBackend/Brasero.cs \
	Banshee.GnomeBackend/DefaultApplicationHelper.cs \
	Banshee.GnomeBackend/DiscDuplicator.cs \
	Banshee.GnomeBackend/GConfConfigurationClient.cs \
	Banshee.GnomeBackend/GnomeScreensaverManager.cs \
	Banshee.GnomeBackend/GnomeService.cs

RESOURCES =  \
	Banshee.Gnome.addin.xml \
	Resources/GlobalUI.xml

if ENABLE_GNOME
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

