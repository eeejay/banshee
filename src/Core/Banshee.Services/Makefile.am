include $(top_srcdir)/build/Common.Makefile

BANSHEE_SERVICES= Banshee.Services
BANSHEE_SERVICES_ASSEMBLY = $(BANSHEE_SERVICES).dll

bansheeservicesdir = $(pkglibdir)
bansheeservices_SCRIPTS = $(BANSHEE_SERVICES_ASSEMBLY) $(BANSHEE_SERVICES_ASSEMBLY).mdb

BANSHEE_SERVICES_SOURCE = \
	$(wildcard Banshee.Collection/*.cs) \
	$(wildcard Banshee.Collection.Database/*.cs) \
	$(wildcard Banshee.Database/*.cs) \
	$(wildcard Banshee.ServiceStack/*.cs)

#	$(wildcard Banshee.Library/*.cs) \
#	$(wildcard Banshee.Playlist/*.cs) \
#	$(wildcard Banshee.Sources/*.cs)

BANSHEE_SERVICES_SOURCE_BUILD = $(addprefix $(srcdir)/, $(BANSHEE_SERVICES_SOURCE))

all: $(BANSHEE_SERVICES_ASSEMBLY)

$(BANSHEE_SERVICES_ASSEMBLY): $(BANSHEE_SERVICES_SOURCE_BUILD)
	$(BUILD_LIB) -out:$@ $(LINK_BANSHEE_CORE_ASM) $(LINK_DBUS) $(LINK_SQLITE) $(LINK_MONO_UNIX) $(LINK_HYENA) $(BANSHEE_SERVICES_SOURCE_BUILD)

EXTRA_DIST = \
	$(BANSHEE_SERVICES_SOURCE)

CLEANFILES = $(BANSHEE_SERVICES_ASSEMBLY) *.mdb *.dll *.pidb
MAINTAINERCLEANFILES = Makefile.in

