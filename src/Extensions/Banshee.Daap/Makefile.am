ASSEMBLY = Banshee.Daap
TARGET = library
LINK = $(REF_EXTENSION_PLAYQUEUE) $(LINK_MONOZEROCONF) $(LINK_ICSHARP_ZIP_LIB) $(LINK_SYSTEM_WEB) -r:/home/alex/Desktop/Downloads/mono-zeroconf-0.7.5/src/Mono.Zeroconf/Mono.Zeroconf.dll
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES = Banshee.Daap/DaapService.cs \
    Banshee.Daap/DaapContainerSource.cs \
    Banshee.Daap/DaapSource.cs \
    daap-sharp/*.cs

RESOURCES = daap-sharp/content-codes \
    Resources/Banshee.Daap.addin.xml

include $(top_srcdir)/build/build.mk

