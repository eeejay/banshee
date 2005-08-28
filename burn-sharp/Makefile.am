MCS_FLAGS = -debug -nowarn:0169
ASSEMBLY_NAME = burn-sharp
ASSEMBLY = $(ASSEMBLY_NAME).dll

burndir = $(pkglibdir)
burn_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).config

include burn-sharp.sources

INCLUDES = $(LIBNAUTILUS_BURN_CFLAGS)
burngluelibdir = $(pkglibdir)
burngluelib_LTLIBRARIES = libnautilusburnglue.la
libnautilusburnglue_la_LDFLAGS = -avoid-version -module
libnautilusburnglue_la_SOURCES = glue.c
libnautilusburnglue_la_LIBADD = $(LIBNAUTILUS_BURN_LIBS)

all: $(ASSEMBLY)

$(ASSEMBLY): $(ASSEMBLY_SOURCES)
	$(MCS) $(MCS_FLAGS) -pkg:gtk-sharp-2.0 -unsafe -target:library -out:$@ $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) burn-sharp.sources $(ASSEMBLY).config.in

CLEANFILES = $(ASSEMBLY)
DISTCLEANFILES = Makefile.in *.mdb *.dll *.exe


