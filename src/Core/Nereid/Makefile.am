include $(top_srcdir)/build/Common.Makefile

NEREID = Nereid
NEREID_ASSEMBLY = $(NEREID).exe

nereiddir = $(pkglibdir)
nereid_SCRIPTS = $(NEREID_ASSEMBLY) $(NEREID_ASSEMBLY).mdb

NEREID_SOURCE = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(wildcard Nereid/*.cs) 
NEREID_SOURCE_BUILD = $(addprefix $(srcdir)/, $(NEREID_SOURCE))

NEREID_RESOURCES = $(wildcard Resources/*)
NEREID_EXPANDED_RESOURCES = $(foreach resource,$(NEREID_RESOURCES),$(srcdir)/$(resource))
NEREID_EXPANDED_RESOURCES_BUILD = $(foreach resource,$(NEREID_EXPANDED_RESOURCES), $(addprefix -resource:,$(resource)),$(notdir $(resource)))

all: $(NEREID_ASSEMBLY)

$(NEREID_ASSEMBLY): $(NEREID_SOURCE_BUILD) $(NEREID_EXPANDED_RESOURCES)
	$(BUILD) -unsafe -out:$@ $(LINK_BANSHEE_CORE_ASM) $(LINK_BANSHEE_SERVICES) $(LINK_HYENA) $(LINK_HYENA_GUI) $(LINK_MONO_UNIX) $(LINK_CAIRO) $(LINK_GTK) $(LINK_GNOME) $(NEREID_EXPANDED_RESOURCES_BUILD) $(NEREID_SOURCE_BUILD)

EXTRA_DIST = \
	$(NEREID_SOURCE) \
	$(NEREID_RESOURCES) 

CLEANFILES = $(NEREID_ASSEMBLY) *.mdb *.exe *.pidb
MAINTAINERCLEANFILES = Makefile.in

