desktopdir = $(datadir)/applications
desktop_in_files = $(wildcard *.desktop.in.in)
desktop_DATA = $(desktop_in_files:.desktop.in.in=.desktop)

DESKTOP_FRAMEWORK_DEPS = common.desktop update-desktop-file.sh
DESKTOP_MIME_DEPS = $(wildcard *.desktop.mime)

if ENABLE_RELEASE
UPDATE_MIME_FILES = no
else
UPDATE_MIME_FILES = yes
endif

%.desktop.in: %.desktop.in.in $(DESKTOP_FRAMEWORK_DEPS) $(DESKTOP_MIME_DEPS)
	./update-desktop-file.sh "$<" "$(VERSION)" "$(UPDATE_MIME_FILES)" > "$@"

@INTLTOOL_DESKTOP_RULE@

EXTRA_DIST = \
	$(desktop_in_files) \
	$(DESKTOP_MIME_DEPS) \
	$(DESKTOP_FRAMEWORK_DEPS)

CLEANFILES = \
	$(desktop_DATA)

MAINTAINERCLEANFILES = \
	Makefile.in

