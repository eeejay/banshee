ASSEMBLY = Banshee.BooScript
TARGET = library
LINK = $(REF_EXTENSION_BOOSCRIPT)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = Banshee.BooScript/BooScriptService.cs

RESOURCES =  \
	Banshee.BooScript.addin.xml \
	Resources/duration-status-formats.boo \
	Resources/filename-transform.boo

if HAVE_BOO
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

