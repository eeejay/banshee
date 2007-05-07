include $(top_srcdir)/build/Common.Makefile

ASSEMBLY = Banshee.Plugins.Sample.dll

SOURCES = \
	$(srcdir)/SamplePlugin.cs \
	$(top_srcdir)/src/AssemblyInfo.cs

all: $(ASSEMBLY)

$(ASSEMBLY): $(SOURCES)
	$(BUILD_LIB_BANSHEE_CORE) $(CUSTOM_MCS_FLAGS) -out:$@ $(LINK_GTK) $(LINK_MONO_UNIX) $(SOURCES)

EXTRA_DIST = $(SOURCES)
CLEANFILES = *.dll *.mdb
MAINTAINERCLEANFILES = Makefile.in *.pidb

