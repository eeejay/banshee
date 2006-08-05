MCS_FLAGS = -debug 
ASSEMBLY_NAME = Banshee.Dap.MassStorage
ASSEMBLY = $(ASSEMBLY_NAME).dll

CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb
MAINTAINERCLEANFILES = Makefile.in 

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/MassStorageDap.cs	\
	$(srcdir)/MassStorageTrackInfo.cs

bansheedapdir = $(pkglibdir)/Banshee.Dap
bansheedap_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll -r:$(top_builddir)/src/Banshee.Widgets/Banshee.Widgets.dll -r:$(top_builddir)/hal-sharp/hal-sharp.dll -r:Mono.Posix -r:$(top_builddir)/entagged-sharp/entagged-sharp.dll $(GTKSHARP_LIBS) $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES)

