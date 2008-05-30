ASSEMBLY = Banshee.Dap.Ipod
TARGET = library
LINK = $(REF_DAP_IPOD)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Dap.Ipod/DatabaseRebuilder.cs \
	Banshee.Dap.Ipod/IpodSource.cs \
	Banshee.Dap.Ipod/IpodTrackInfo.cs \
	Banshee.Dap.Ipod/PodSleuthDevice.cs \
	Banshee.Dap.Ipod/PodSleuthDeviceProvider.cs \
	Banshee.Dap.Ipod/UnsupportedDatabaseView.cs

RESOURCES = Banshee.Dap.Ipod.addin.xml

EXTRA_BUNDLE = $(IPODSHARP_ASSEMBLIES)

if ENABLE_IPOD
include $(top_srcdir)/build/build.mk

install-data-hook:
	for ASM in $(IPODSHARP_ASSEMBLIES); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(moduledir); \
	done;

uninstall-hook:
	for ASM in $(IPODSHARP_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(moduledir)/`basename $$ASM`; \
	done;

else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

