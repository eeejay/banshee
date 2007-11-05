include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = Banshee.MediaEngine.GStreamer
ASSEMBLY = $(ASSEMBLY_NAME).dll

gstenginelibdir = $(pkglibdir)/Banshee.MediaEngine
gstenginelib_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb 

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/GstPlayerEngine.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(BUILD_LIB) -out:$@ $(LINK_BANSHEE_CORE_ASM) $(LINK_BANSHEE_SERVICES) $(LINK_HYENA) $(LINK_MONO_ADDINS_CORE) $(LINK_GLIB) $(LINK_MONO_UNIX) $(ASSEMBLY_SOURCES)

MAINTAINERCLEANFILES = Makefile.in
CLEANFILES = *.exe *.dll *.mdb *.pidb

EXTRA_DIST = $(ASSEMBLY_SOURCES)

