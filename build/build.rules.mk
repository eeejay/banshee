UNIQUE_FILTER_PIPE = tr [:space:] \\n | sort | uniq
BUILD_DATA_DIR = $(top_builddir)/bin/share/$(PACKAGE)

SOURCES_BUILD = $(addprefix $(srcdir)/, $(SOURCES))
SOURCES_BUILD += $(top_srcdir)/src/AssemblyInfo.cs

RESOURCES_EXPANDED = $(addprefix $(srcdir)/, $(RESOURCES))
RESOURCES_BUILD = $(foreach resource, $(RESOURCES_EXPANDED), \
	-resource:$(resource),$(notdir $(resource)))

INSTALL_ICONS = $(top_srcdir)/build/private-icon-theme-installer "$(mkinstalldirs)" "$(INSTALL_DATA)"
THEME_ICONS_SOURCE = $(wildcard $(srcdir)/ThemeIcons/*/*/*.png)
THEME_ICONS_RELATIVE = $(subst $(srcdir)/ThemeIcons/, , $(THEME_ICONS_SOURCE))

CONFIG_FILES_IN = $(wildcard $(srcdir)/*.config.in)

ASSEMBLY_EXTENSION = $(strip $(patsubst library, dll, $(TARGET)))
ASSEMBLY_FILE = $(top_builddir)/bin/$(ASSEMBLY).$(ASSEMBLY_EXTENSION)

INSTALL_DIR_RESOLVED = $(firstword $(subst , $(DEFAULT_INSTALL_DIR), $(INSTALL_DIR)))

FILTERED_LINK = $(shell echo "$(LINK)" | $(UNIQUE_FILTER_PIPE))
DEP_LINK = $(shell echo "$(LINK)" | $(UNIQUE_FILTER_PIPE) | sed s,-r:,,g | grep '$(top_builddir)/bin/')

OUTPUT_FILES = \
	$(ASSEMBLY_FILE) \
	$(ASSEMBLY_FILE).mdb

moduledir = $(INSTALL_DIR_RESOLVED)
module_SCRIPTS = $(OUTPUT_FILES)

all: $(ASSEMBLY_FILE) theme-icons

build-debug:
	@echo $(DEP_LINK)

$(ASSEMBLY_FILE): $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(DEP_LINK)
	@mkdir -p $(top_builddir)/bin
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

theme-icons: $(THEME_ICONS_SOURCE)
	@$(INSTALL_ICONS) -il "$(BUILD_DATA_DIR)" "$(srcdir)" $(THEME_ICONS_RELATIVE)

install-data-local: $(THEME_ICONS_SOURCE)
	@$(INSTALL_ICONS) -i "$(DESTDIR)$(pkgdatadir)" "$(srcdir)" $(THEME_ICONS_RELATIVE)
	
uninstall-local: $(THEME_ICONS_SOURCE)
	@$(INSTALL_ICONS) -u "$(DESTDIR)$(pkgdatadir)" "$(srcdir)" $(THEME_ICONS_RELATIVE)

EXTRA_DIST = $(SOURCES_BUILD) $(RESOURCES_EXPANDED) $(THEME_ICONS_SOURCE) $(CONFIG_FILES_IN)

CLEANFILES = $(OUTPUT_FILES) *.dll *.mdb *.exe
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in

