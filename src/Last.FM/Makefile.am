include $(top_srcdir)/build/Common.Makefile

ASSEMBLY_NAME = Last.FM
ASSEMBLY = $(ASSEMBLY_NAME).dll

last_fmdir= $(pkglibdir)
last_fm_SCRIPTS = $(ASSEMBLY) $(ASSEMBLY).mdb

LAST_FM_SOURCES = \
	$(srcdir)/Last.FM/Account.cs \
	$(srcdir)/Last.FM/Browser.cs

LAST_FM_GUI_SOURCES = \
	$(srcdir)/Last.FM.Gui/AccountLoginForm.cs \
	$(srcdir)/Last.FM.Gui/AccountLoginDialog.cs \
	$(srcdir)/Last.FM.Gui/Badge.cs \
	$(srcdir)/Last.FM.Gui/LinkButton.cs

ASSEMBLY_SOURCES = \
	$(LAST_FM_SOURCES) \
	$(LAST_FM_GUI_SOURCES)

$(ASSEMBLY): $(ASSEMBLY_SOURCES) $(srcdir)/badge.png $(srcdir)/badge-hover.png
	$(BUILD_LIB) -out:$@ $(LINK_MONO_UNIX) $(LINK_GNOME_KEYRING) $(LINK_GTK) -resource:$(srcdir)/badge.png,badge.png -resource:$(srcdir)/badge-hover.png,badge-hover.png $(ASSEMBLY_SOURCES)

EXTRA_DIST = $(ASSEMBLY_SOURCES) badge.png badge-hover.png

CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in

run: test
	mono last-fm-test.exe

test: $(ASSEMBLY_SOURCES) Test.cs
	 mcs ../Gnome.Keyring/*.cs $(ASSEMBLY_SOURCES) Test.cs -out:last-fm-test.exe -resource:badge.png,badge.png -resource:badge-hover.png,badge-hover.png -pkg:gtk-sharp-2.0 -r:Mono.Posix -pkg:gnome-sharp-2.0

