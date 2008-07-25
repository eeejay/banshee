SUBDIRS = pkg-config m4

GCONF_SCHEMA_EXTRACTOR_ASSEMBLY = gconf-schema-extractor.exe
TRANSLATOR_EXTRACTOR_ASSEMBLY = translator-extractor.exe

bansheedir = $(pkglibdir)
banshee_SCRIPTS = $(GCONF_SCHEMA_EXTRACTOR_ASSEMBLY)

all: $(GCONF_SCHEMA_EXTRACTOR_ASSEMBLY) $(TRANSLATOR_EXTRACTOR_ASSEMBLY)

$(GCONF_SCHEMA_EXTRACTOR_ASSEMBLY): GConfSchemaExtractor.cs
	$(MCS) -out:$@ $<

$(TRANSLATOR_EXTRACTOR_ASSEMBLY): TranslatorExtractor.cs
	if [ "x$(top_srcdir)" = "x$(top_builddir)" ]; then \
		$(MCS) -out:$@ $< && LC_ALL=en_US.utf8 $(MONO) $@ $(top_builddir)/po > \
			$(top_srcdir)/src/Core/Banshee.Core/Resources/translators.xml; \
	fi;

EXTRA_DIST = \
	icon-theme-installer \
	private-icon-theme-installer \
	GConfSchemaExtractor.cs \
	TranslatorExtractor.cs \
	gconf-schema-rules

CLEANFILES = *.exe *.mdb
MAINTAINERCLEANFILES = Makefile.in

