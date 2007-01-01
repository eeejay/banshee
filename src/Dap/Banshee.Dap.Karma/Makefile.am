include $(top_srcdir)/build/Common.Makefile
ASSEMBLY_NAME = Banshee.Dap.Karma
ASSEMBLY = $(ASSEMBLY_NAME).dll

CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb
MAINTAINERCLEANFILES = Makefile.in

ASSEMBLY_SOURCES = \
	$(srcdir)/Karma.cs \
	$(srcdir)/KarmaDapTrackInfo.cs

if ENABLE_KARMA

bansheedapdir = $(pkglibdir)/Banshee.Dap
bansheedap_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb karma-sharp.dll.config

endif

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(BUILD_LIB_BANSHEE_CORE) -out:$@ $(LINK_HAL) $(LINK_GTK) $(LINK_MONO_UNIX) $(KARMASHARP_LIBS) $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) karma-sharp.dll.config

install-data-hook:
	for ASM in $(KARMASHARP_ASSEMBLIES); do \
		$(INSTALL) -m 0755 $$ASM $(DESTDIR)$(bansheedapdir); \
	done;

uninstall-hook:
	for ASM in $(KARMASHARP_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(bansheedapdir)/`basename $$ASM`; \
	done;

