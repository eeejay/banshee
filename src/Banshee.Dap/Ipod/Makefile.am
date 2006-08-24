MCS_FLAGS = -debug 
ASSEMBLY_NAME = Banshee.Dap.Ipod
ASSEMBLY = $(ASSEMBLY_NAME).dll
EXTRA_DIST = $(ASSEMBLY_SOURCES) ipod-sharp.dll.config.in
CLEANFILES = $(ASSEMBLY) *.dll *.exe 
DISTCLEANFILES = *.mdb
MAINTAINERCLEANFILES = Makefile.in

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/IpodDap.cs \
	$(srcdir)/IpodDapTrackInfo.cs \
	$(srcdir)/UnsupportedDatabaseView.cs \
	$(srcdir)/DatabaseRebuilder.cs

if ENABLE_IPOD

bansheedapdir = $(pkglibdir)/Banshee.Dap
bansheedap_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb ipod-sharp.dll.config

endif

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll -r:$(top_builddir)/src/Banshee.Widgets/Banshee.Widgets.dll -r:$(top_builddir)/hal-sharp/hal-sharp.dll -r:$(top_builddir)/taglib-sharp/taglib-sharp.dll -r:Mono.Posix $(GTKSHARP_LIBS) $(IPODSHARP_LIBS) $(ASSEMBLY_SOURCES)

install-data-hook:
	for ASM in $(IPODSHARP_ASSEMBLIES); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(bansheedapdir); \
	done;
	
uninstall-hook:
	for ASM in $(IPODSHARP_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(bansheedapdir)/`basename $$ASM`; \
	done;


