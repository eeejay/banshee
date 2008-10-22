ASSEMBLY = Banshee.Osx
TARGET = library
LINK = $(REF_BACKEND_OSX)
INSTALL_DIR = $(BACKENDS_INSTALL_DIR)

SOURCES =  \
	Banshee.OsxBackend/OsxService.cs \
	Banshee.OsxBackend/HardwareManager.cs 

RESOURCES =  \
	Banshee.Osx.addin.xml \
	Resources/osx-ui-actions-layout.xml

if ENABLE_OSX
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

MAINTAINERCLEANFILES = Makefile.in
