include $(top_srcdir)/build/Common.Makefile

BANSHEE_CORE = Banshee.Core
BANSHEE_CORE_ASSEMBLY = $(BANSHEE_CORE).dll

bansheecoredir = $(pkglibdir)
bansheecore_SCRIPTS = $(BANSHEE_CORE_ASSEMBLY) $(BANSHEE_CORE_ASSEMBLY).mdb

BANSHEE_CORE_SOURCE = \
	$(wildcard Banshee.Kernel/*.cs) \
	$(wildcard Banshee.Base/*.cs) \
	$(wildcard Banshee.Collection/*.cs) \
	$(wildcard Banshee.Configuration/*.cs) \
	$(wildcard Banshee.Configuration.Schema/*.cs) \
	$(wildcard Banshee.IO/*.cs) 

BANSHEE_CORE_SOURCE_BUILD = $(addprefix $(srcdir)/, $(BANSHEE_CORE_SOURCE))

all: $(BANSHEE_CORE_ASSEMBLY)

$(BANSHEE_CORE_ASSEMBLY): $(BANSHEE_CORE_SOURCE_BUILD)
	$(BUILD_LIB) -out:$@ $(GCONFSHARP_LIBS) $(LINK_TAGLIB) $(LINK_DBUS) $(LINK_HYENA) $(LINK_MONO_UNIX) $(BANSHEE_CORE_SOURCE_BUILD)

EXTRA_DIST = \
	$(BANSHEE_CORE_SOURCE) \
	ConfigureDefines.cs.in

CLEANFILES = $(BANSHEE_CORE_ASSEMBLY) *.mdb *.dll *.pidb
MAINTAINERCLEANFILES = Makefile.in

