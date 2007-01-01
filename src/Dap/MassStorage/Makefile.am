include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = Banshee.Dap.MassStorage
ASSEMBLY = $(ASSEMBLY_NAME).dll

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/MassStorageDap.cs	\
	$(srcdir)/MassStorageTrackInfo.cs

bansheedapdir = $(pkglibdir)/Banshee.Dap
bansheedap_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(BUILD_LIB_BANSHEE_CORE) -out:$@ $(LINK_HAL) $(LINK_GTK) $(LINK_MONO_UNIX) $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES)
CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = $(ASSEMBLY).mdb
MAINTAINERCLEANFILES = Makefile.in

