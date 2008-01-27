include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = Banshee.Plugins.Audioscrobbler
ASSEMBLY = $(ASSEMBLY_NAME).dll
ASSEMBLY_GCONF_SCHEMA = banshee-plugin-audioscrobbler.schemas.in

audioscrobblerdir = $(pkglibdir)/Banshee.Plugins
audioscrobbler_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb $(ASSEMBLY).config

MENU_RESOURCE = AudioscrobblerMenu.xml

ASSEMBLY_SOURCES = \
	$(top_srcdir)/src/AssemblyInfo.cs \
	$(srcdir)/AudioscrobblerPlugin.cs \
	$(srcdir)/AudioscrobblerConfigDialog.cs \
	$(srcdir)/Engine.cs \
	$(srcdir)/Queue.cs

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(MENU_RESOURCE)
	$(BUILD_LIB_BANSHEE_CORE) -out:$@ $(LINK_LAST_FM) $(LINK_GTK) $(LINK_MONO_UNIX) -r:System.Web -r:Mono.Security -resource:$(srcdir)/$(MENU_RESOURCE),$(MENU_RESOURCE) -resource:$(srcdir)/audioscrobbler-logo.png,audioscrobbler-logo.png $(ASSEMBLY_SOURCES) 
	
include $(top_srcdir)/build/gconf-schema-rules

EXTRA_DIST = \
	$(ASSEMBLY_SOURCES) \
	$(ASSEMBLY).config.in \
	$(MENU_RESOURCE) \
	audioscrobbler-logo.png \
	$(ASSEMBLY_GCONF_SCHEMA)

CLEANFILES = $(ASSEMBLY) *.dll *.exe
DISTCLEANFILES = *.mdb $(schema_DATA)
MAINTAINERCLEANFILES = Makefile.in

