ASSEMBLY = Banshee.Dap.Karma
TARGET = library
LINK = $(REF_DAP_KARMA)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Dap.Karma/KarmaSource.cs \
	Banshee.Dap.Karma/KarmaTrackInfo.cs

RESOURCES = Banshee.Dap.Karma.addin.xml

EXTRA_BUNDLE = $(KARMASHARP_ASSEMBLIES)

if ENABLE_KARMA
include $(top_srcdir)/build/build.mk

install-data-hook:
	for ASM in $(KARMASHARP_ASSEMBLIES); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;

uninstall-hook:
	for ASM in $(KARMASHARP_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
