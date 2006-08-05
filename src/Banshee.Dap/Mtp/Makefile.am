MCS_FLAGS = -debug
ASSEMBLY_NAME = Banshee.Dap.Mtp
ASSEMBLY = $(ASSEMBLY_NAME).dll
EXTRA_DIST = $(ASSEMBLY_SOURCES)

CLEANFILES = $(ASSEMBLY) *.dll *.exe *.mdb
DISTCLEANFILES = *.mdb
MAINTAINERCLEANFILES = Makefile.in 

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/MtpDap.cs \
	$(srcdir)/MtpDapTrackInfo.cs \
	$(srcdir)/MtpDeviceId.cs \
	$(srcdir)/GPhotoDevice.cs \
	$(srcdir)/GPhotoDeviceFile.cs

if ENABLE_MTP
bansheedapdir = $(pkglibdir)/Banshee.Dap
bansheedap_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb
endif

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -target:library -out:$@ -r:$(top_builddir)/src/Banshee.Base/Banshee.Base.dll \
	-r:$(top_builddir)/src/Banshee.Widgets/Banshee.Widgets.dll -r:$(top_builddir)/hal-sharp/hal-sharp.dll \
	-r:Mono.Posix $(GTKSHARP_LIBS) $(LIBGPHOTO2SHARP_LIBS) $(ASSEMBLY_SOURCES)

install-data-hook:
	for cur in $(LIBGPHOTO2SHARP_ASSEMBLIES); do \
		$(INSTALL) -m 0755 $$cur $(DESTDIR)$(bansheedapdir); \
		$(INSTALL) -m 0755 $${cur}.mdb $(DESTDIR)$(bansheedapdir); \
		$(INSTALL) -m 0755 $${cur}.config $(DESTDIR)$(bansheedapdir); \
	done;

uninstall-hook:
	for cur in $(LIBGPHOTO2SHARP_ASSEMBLIES); do \
		rm -f $(DESTDIR)$(bansheedapdir)/`basename $$cur`; \
		rm -f $(DESTDIR)$(bansheedapdir)/`basename $${cur}.mdb`; \
		rm -f $(DESTDIR)$(bansheedapdir)/`basename $${cur}.config`; \
	done;
	

