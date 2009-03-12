SUBDIRS = pkg-config m4

GCONF_SCHEMA_EXTRACTOR_ASSEMBLY = gconf-schema-extractor.exe
TRANSLATOR_EXTRACTOR_ASSEMBLY = translator-extractor.exe
DLL_MAP_VERIFIER_ASSEMBLY = dll-map-verifier.exe

ALL_TARGETS = $(TRANSLATOR_EXTRACTOR_ASSEMBLY) $(DLL_MAP_VERIFIER_ASSEMBLY)

if GCONF_SCHEMAS_INSTALL
bansheedir = $(pkglibdir)
banshee_SCRIPTS = $(GCONF_SCHEMA_EXTRACTOR_ASSEMBLY)
ALL_TARGETS += $(GCONF_SCHEMA_EXTRACTOR_ASSEMBLY)
$(GCONF_SCHEMA_EXTRACTOR_ASSEMBLY): GConfSchemaExtractor.cs
	$(MCS) -out:$@ $<
endif

all: $(ALL_TARGETS)

$(TRANSLATOR_EXTRACTOR_ASSEMBLY): TranslatorExtractor.cs
	if [ "x$(top_srcdir)" = "x$(top_builddir)" ]; then \
		$(MCS) -out:$@ $< && LC_ALL=en_US.utf8 $(MONO) $@ $(top_builddir)/po > \
			$(top_srcdir)/src/Core/Banshee.Core/Resources/translators.xml; \
	fi;

$(DLL_MAP_VERIFIER_ASSEMBLY): DllMapVerifier.cs
	$(MCS) -out:$@ $<

EXTRA_DIST = \
	icon-theme-installer \
	private-icon-theme-installer \
	GConfSchemaExtractor.cs \
	TranslatorExtractor.cs \
	DllMapVerifier.cs \
	dll-map-makefile-verifier \
	gconf-schema-rules

CLEANFILES = *.exe *.mdb
MAINTAINERCLEANFILES = Makefile.in

