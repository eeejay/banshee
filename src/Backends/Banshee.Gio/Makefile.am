ASSEMBLY = Banshee.Gio
TARGET = library
LINK = $(REF_BACKEND_GIO)
INSTALL_DIR = $(BACKENDS_INSTALL_DIR)

SOURCES =  \
	Banshee.IO.Gio/DemuxVfs.cs \
	Banshee.IO.Gio/Directory.cs \
	Banshee.IO.Gio/File.cs \
	Banshee.IO.Gio/Provider.cs \
	Banshee.IO.Gio/Tests.cs

RESOURCES =  \
	Banshee.Gio.addin.xml

if ENABLE_GIO
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

