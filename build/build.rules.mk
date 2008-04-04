SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))
SOURCES_BUILD += $(top_srcdir)/src/AssemblyInfo.cs

RESOURCES_EXPANDED = $(addprefix $(srcdir)/, $(RESOURCES))
RESOURCES_BUILD = $(foreach resource, $(RESOURCES_EXPANDED), \
	-resource:$(resource),$(notdir $(resource)))

THEME_ICONS = $(wildcard $(srcdir)/ThemeIcons/*/*/*.png)

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(top_builddir)/bin/$(ASSEMBLY).$(ASSEMBLY_EXTENSION)

INSTALL_DIR_RESOLVED = $(firstword $(subst , $(DEFAULT_INSTALL_DIR), $(INSTALL_DIR)))

FILTER_LINK_PIPE = tr [:space:] \\n | sort | uniq
FILTERED_LINK = $(shell echo "$(LINK)" | $(FILTER_LINK_PIPE))
DEP_LINK = $(shell echo "$(LINK)" | $(FILTER_LINK_PIPE) | sed s,-r:,,g | grep '$(top_builddir)/bin/')

OUTPUT_FILES = \
	$(ASSEMBLY_FILE) \
	$(ASSEMBLY_FILE).mdb

moduledir = $(INSTALL_DIR_RESOLVED)
module_SCRIPTS = $(OUTPUT_FILES)

all: $(ASSEMBLY_FILE)

build-debug:
	@echo $(DEP_LINK)

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(DEP_LINK)
	@mkdir -p $(top_builddir)/bin
	@(test -d $(srcdir)/ThemeIcons && mkdir -p $(top_builddir)/bin/share/$(PACKAGE)/icons/hicolor && cp -rf $(srcdir)/ThemeIcons/* $(top_builddir)/bin/share/$(PACKAGE)/icons/hicolor) || true
	@colors=no; \
	case $$TERM in \
		"xterm" | "rxvt" | "rxvt-unicode") \
			test "x$$COLORTERM" != "x" && colors=yes ;; \
		"xterm-color") colors=yes ;; \
	esac; \
	if [ "x$$colors" = "xyes" ]; then \
		tty -s && true || { colors=no; true; } \
	fi; \
	test "x$$colors" = "xyes" && \
		echo -e "\033[1mCompiling $(notdir $@)...\033[0m" || \
		echo "Compiling $(notdir $@)...";
	@test "x$(HAVE_MONO_1_2_4)" = "xyes" && warn="-warnaserror"; test "x$(HAVE_GTK_2_10)" = "xyes" && gtk_210="-define:HAVE_GTK_2_10"; $(BUILD) -target:$(TARGET) -out:$@ $$warn $$gtk_210 $(FILTERED_LINK) $(RESOURCES_BUILD) $(SOURCES_BUILD) 
	@if [ -e $(notdir $@.config) ]; then \
		cp $(notdir $@.config) $(top_builddir)/bin; \
	fi;

EXTRA_DIST = $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(THEME_ICONS)

CLEANFILES = $(OUTPUT_FILES) *.dll *.mdb *.exe
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in
