ASSEMBLY = Last.FM
TARGET = library
LINK = $(REF_LAST_FM)

SOURCES =  \
	Last.FM.Gui/AccountLoginDialog.cs \
	Last.FM.Gui/AccountLoginForm.cs \
	Last.FM.Gui/Badge.cs \
	Last.FM.Gui/LinkButton.cs \
	Last.FM/Account.cs \
	Last.FM/Browser.cs

RESOURCES =  \
	Resources/badge.png \
	Resources/badge-hover.png

include $(top_srcdir)/build/build.mk

EXTRA_DIST += Test.cs

run: test
	MONO_PATH=$(DIR_GNOME_KEYRING) $(MONO) last-fm-test.exe

test: last-fm-test.exe

last-fm-test.exe: $(SOURCES_BUILD) $(RESOURCES_EXPANDED_FULL) Test.cs
	$(BUILD) $(LINK_LAST_FM_DEPS) -out:$@ Test.cs

