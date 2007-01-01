include $(top_srcdir)/build/Common.Makefile

SUBDIRS = m4

GCONF_SCHEMA_EXTRACTOR_ASSEMBLY = gconf-schema-extractor.exe

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = banshee.pc

bansheedir = $(pkglibdir)
banshee_SCRIPTS = $(GCONF_SCHEMA_EXTRACTOR_ASSEMBLY)

all: $(GCONF_SCHEMA_EXTRACTOR_ASSEMBLY)

$(GCONF_SCHEMA_EXTRACTOR_ASSEMBLY): GConfSchemaExtractor.cs
	$(BUILD) -out:$@ $<

EXTRA_DIST = \
	Common.Makefile \
	mcs-test-79698.cs \
	gmcs.exe \
	icon-theme-installer \
	GConfSchemaExtractor.cs \
	gconf-schema-rules \
	banshee.pc.in

CLEANFILES = $(GCONF_SCHEMA_EXTRACTOR_ASSEMBLY){,.mdb} 
DISTCLEANFILES = banshee.pc
MAINTAINERCLEANFILES = Makefile.in

