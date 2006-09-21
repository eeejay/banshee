include $(top_srcdir)/Makefile.include

ASSEMBLY_NAME = Banshee.Dap.Njb
ASSEMBLY = $(ASSEMBLY_NAME).dll

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/NjbDap.cs \
	$(srcdir)/NjbDapTrackInfo.cs

if ENABLE_NJB
bansheedapdir = $(pkglibdir)/Banshee.Dap
bansheedap_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb njb-sharp.dll.config
endif

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(BUILD_LIB_BANSHEE_CORE) -out:$@ $(LINK_HAL) $(LINK_GTK) $(LINK_MONO_UNIX) $(NJBSHARP_LIBS) $(ASSEMBLY_SOURCES)

install-data-hook:
	for i in $(NJBSHARP_INSTALL_FILES); do \
		$(INSTALL) -m 0755 $$i $(DESTDIR)$(bansheedapdir); \
	done;

uninstall-hook:
	for i in $(NJBSHARP_INSTALL_FILES); do \
		rm -f $(DESTDIR)$(bansheedapdir)/`basename $$i`; \
	done;
	
EXTRA_DIST = $(ASSEMBLY_SOURCES) njb-sharp.dll.config.in
CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = $(ASSEMBLY).mdb
MAINTAINERCLEANFILES = Makefile.in 


