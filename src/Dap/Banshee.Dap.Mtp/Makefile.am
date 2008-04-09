ASSEMBLY = Banshee.Dap.Mtp
TARGET = library
LINK = $(REF_DAP_MTP)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = Banshee.Dap.Mtp/MtpSource.cs

RESOURCES = Banshee.Dap.Mtp.addin.xml

if ENABLE_MTP
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
