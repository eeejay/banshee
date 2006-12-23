SUBDIRS = \
	images

AUDIO_PROFILES = \
	$(srcdir)/audio-profiles/base.xml \
	$(srcdir)/audio-profiles/aac.xml \
	$(srcdir)/audio-profiles/flac.xml \
	$(srcdir)/audio-profiles/mp3-lame.xml \
	$(srcdir)/audio-profiles/mp3-xing.xml \
	$(srcdir)/audio-profiles/vorbis.xml \
	$(srcdir)/audio-profiles/wav.xml \
	$(srcdir)/audio-profiles/wma.xml 

audioprofilesdir = $(pkgdatadir)/audio-profiles
audioprofiles_DATA = $(AUDIO_PROFILES)

desktopdir = $(datadir)/applications
desktop_in_files=banshee.desktop.in.in
desktop_DATA=$(desktop_in_files:.desktop.in.in=.desktop)

%.desktop.in: %.desktop.in.in
	sed -e "s/\@VERSION\@/$(VERSION)/" < $< > $@

@INTLTOOL_DESKTOP_RULE@

dbusservice_in_files = org.gnome.Banshee.service.in
dbusservicedir = $(datadir)/dbus-1/services
dbusservice_DATA = $(dbusservice_in_files:.service.in=.service)

all: translator-extract.exe

translator-extract.exe: TranslatorExtract.cs
	$(MCS) -out:$@ $< && $(MONO) $@ $(top_builddir)/po > translators.xml

CLEANFILES = \
	*.gladep \
	*.bak \
	translators.xml \
	translator-extract.exe

EXTRA_DIST = \
	UIManagerLayout.xml \
	banshee.glade \
	banshee-dialogs.glade \
	contributors.xml \
	TranslatorExtract.cs \
	$(desktop_in_files) \
	$(dbusservice_in_files) \
	$(AUDIO_PROFILES)

DISTCLEANFILES =	\
	$(dbusservice_DATA) \
	$(desktop_DATA)

MAINTAINERCLEANFILES = \
	Makefile.in


