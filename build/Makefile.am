SUBDIRS = m4

GCONF_SCHEMA_EXTRACTOR_ASSEMBLY = gconf-schema-extractor.exe
TRANSLATOR_EXTRACTOR_ASSEMBLY = translator-extractor.exe

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = banshee.pc

bansheedir = $(pkglibdir)
banshee_SCRIPTS = $(GCONF_SCHEMA_EXTRACTOR_ASSEMBLY)

all: $(GCONF_SCHEMA_EXTRACTOR_ASSEMBLY) $(TRANSLATOR_EXTRACTOR_ASSEMBLY)

$(GCONF_SCHEMA_EXTRACTOR_ASSEMBLY): GConfSchemaExtractor.cs
	$(MCS) -out:$@ $<

$(TRANSLATOR_EXTRACTOR_ASSEMBLY): TranslatorExtractor.cs
	$(MCS) -out:$@ $< && $(MONO) $@ $(top_builddir)/po > \
		$(top_srcdir)/src/Core/Banshee.Core/Resources/translators.xml

EXTRA_DIST = \
	icon-theme-installer \
	GConfSchemaExtractor.cs \
	TranslatorExtractor.cs \
	gconf-schema-rules \
	banshee.pc.in

CLEANFILES = *.exe *.mdb
DISTCLEANFILES = banshee.pc
MAINTAINERCLEANFILES = Makefile.in

