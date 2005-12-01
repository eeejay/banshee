DOC_UPDATER = @MONODOCER@ -delete
DOC_ASSEMBLER = @MDASSEMBLER@ --out banshee-docs --ecma

ASSEMBLIES = \
	$(top_builddir)/hal-sharp/hal-sharp.dll \
	$(top_builddir)/src/Banshee.Base/Banshee.Base.dll \
	$(top_builddir)/src/Banshee.Widgets/Banshee.Widgets.dll

ASSEMBLIES_BUILD = $(foreach asm,$(ASSEMBLIES),$(addprefix -assembly:,$(asm)))

if BUILD_DOCS
docdir = $(DOCDIR)
doc_DATA = \
	banshee-docs.zip \
	banshee-docs.tree \
	banshee-docs.source
endif

update-docs: $(ASSEMBLIES)
	$(DOC_UPDATER) $(ASSEMBLIES_BUILD) -path:en

banshee-docs.zip banshee-docs.tree: $(srcdir)/en/*/*.xml
	$(DOC_ASSEMBLER) $(srcdir)/en

EXTRA_DIST = \
	$(srcdir)/en/*.xml \
	$(srcdir)/en/*/*.xml \
	banshee-docs.source
	
DISTCLEANFILES = Makefile.in banshee-docs.zip banshee-docs.tree

