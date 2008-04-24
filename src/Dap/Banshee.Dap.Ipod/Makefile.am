ASSEMBLY = Banshee.Dap.Ipod
TARGET = library
LINK = $(REF_DAP_IPOD)

SOURCES =  \
	Banshee.Dap.Ipod/IpodSource.cs \
	Banshee.Dap.Ipod/PodSleuthDevice.cs \
	Banshee.Dap.Ipod/PodSleuthDeviceProvider.cs

RESOURCES = Banshee.Dap.Ipod.addin.xml

EXTRA_BUNDLE = $(IPODSHARP_ASSEMBLIES)

if ENABLE_IPOD
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif

