include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/HelixRemotePlayer.cs \
	$(srcdir)/HelixRemotePlayerEngine.cs

if ENABLE_HELIX
	
EXTRA_DIST = $(ASSEMBLY_SOURCES)
MAINTAINERCLEANFILES = Makefile.in
CLEANFILES = *.dll *.mdb *.exe *.pidb

MCS_FLAGS = -debug
ASSEMBLY_NAME = Banshee.MediaEngine.HelixRemote
ASSEMBLY = $(ASSEMBLY_NAME).dll

helixenginedir = $(pkglibdir)/Banshee.MediaEngine
helixengine_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb 

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(BUILD_LIB_BANSHEE_CORE) -out:$@ $(LINK_GTK) $(LINK_MONO_UNIX) $(LINK_DBUS) $(ASSEMBLY_SOURCES)

endif

