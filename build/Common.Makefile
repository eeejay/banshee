## This is a project-wide Makefile helper
## Do not use $(MCS), $(MCS_FLAGS), $(GTKSHARP_LIBS), etc
## directly; please use the variables in this file as they
## will make maintaining the build system much easier

## Directories ##

DIR_HAL = $(top_builddir)/hal-sharp
DIR_TAGLIB = $(top_builddir)/taglib-sharp
DIR_DBUS = $(top_builddir)/dbus-sharp
DIR_LAST_FM = $(top_builddir)/src/Last.FM
DIR_MUSICBRAINZ = $(top_builddir)/src/MusicBrainz
DIR_GNOME_KEYRING = $(top_builddir)/src/Gnome.Keyring
DIR_BANSHEE_WIDGETS = $(top_builddir)/src/Banshee.Widgets
DIR_BANSHEE_BASE = $(top_builddir)/src/Banshee.Base
DIR_BOO = $(top_srcdir)/src/Boo
DIR_BOOBUDDY = $(top_builddir)/src/BooBuddy

DIR_DAP = $(top_builddir)/src/Banshee.Dap
DIR_DAP_IPOD = $(DIR_DAP)/Ipod:$$(dirname `pkg-config --variable=Libraries ipod-sharp`)
DIR_DAP_NJB = $(DIR_DAP)/Njb:$$(dirname `pkg-config --variable=Libraries njb-sharp`)
DIR_DAP_MTP = $(DIR_DAP)/Mtp
DIR_DAP_MASS_STORAGE = $(DIR_DAP)/MassStorage
DIR_DAP_ALL=$(DIR_DAP_IPOD):$(DIR_DAP_NJB):$(DIR_DAP_MTP):$(DIR_DAP_MASS_STORAGE)

## Linking ##

LINK_GTK = $(GTKSHARP_LIBS)
LINK_MONO_UNIX = -r:Mono.Posix

LINK_HAL = -r:$(DIR_HAL)/Hal.dll
LINK_TAGLIB = -r:$(DIR_TAGLIB)/TagLib.dll
LINK_LAST_FM = -r:$(DIR_LAST_FM)/Last.FM.dll
LINK_MUSICBRAINZ = -r:$(DIR_MUSICBRAINZ)/MusicBrainz.dll
LINK_GNOME_KEYRING = -r:$(DIR_GNOME_KEYRING)/Gnome.Keyring.dll
LINK_DBUS = \
	-r:$(DIR_DBUS)/NDesk.DBus.dll \
	-r:$(DIR_DBUS)/NDesk.DBus.GLib.dll

if EXTERNAL_BOO
LINK_BOO = $(BOO_LIBS)
else
LINK_BOO = \
	-r:$(DIR_BOO)/Boo.Lang.Compiler.dll \
	-r:$(DIR_BOO)/Boo.Lang.Interpreter.dll
endif
LINK_BOOBUDDY = -r:$(DIR_BOOBUDDY)/BooBuddy.dll

LINK_BANSHEE_WIDGETS = -r:$(DIR_BANSHEE_WIDGETS)/Banshee.Widgets.dll
LINK_BANSHEE_CORE = \
	$(LINK_BANSHEE_WIDGETS) \
	-r:$(DIR_BANSHEE_BASE)/Banshee.Base.dll

## Building ##

# Ignoring 0278 due to a bug in gmcs: 
# http://bugzilla.ximian.com/show_bug.cgi?id=79998
BUILD_FLAGS = -debug -nowarn:0278
BUILD = $(MCS) $(BUILD_FLAGS)
BUILD_LIB = $(BUILD) -target:library

BUILD_BANSHEE_CORE = \
	MONO_PATH=$(top_builddir)/dbus-sharp \
	$(BUILD) \
	$(LINK_BANSHEE_CORE)
	
BUILD_LIB_BANSHEE_CORE = \
	$(BUILD_BANSHEE_CORE) \
	-target:library

BUILD_UI_BANSHEE = \
	$(BUILD_BANSHEE_CORE) \
	$(LINK_GTK) \
	$(LINK_HAL) \
	$(LINK_MONO_UNIX) \
	$(LINK_DBUS)

## Running ##

RUN_PATH = \
	LD_LIBRARY_PATH=$(top_builddir)/libbanshee/.libs \
	DYLD_LIBRARY_PATH=$${LD_LIBRARY_PATH} \
	MONO_PATH=$(DIR_HAL):$(DIR_TAGLIB):$(DIR_DBUS):$(DIR_LAST_FM):$(DIR_MUSICBRAINZ):$(DIR_GNOME_KEYRING):$(DIR_BANSHEE_WIDGETS):$(DIR_BANSHEE_BASE):$(DIR_BOO):$(DIR_BOOBUDDY):$(DIR_DAP_ALL)${MONO_PATH+:$$MONO_PATH} \
	BANSHEE_ENGINES_PATH=$(top_builddir)/src/Banshee.MediaEngine \
	BANSHEE_PLUGINS_PATH=$(top_builddir)/src/Banshee.Plugins$${BANSHEE_PLUGINS_PATH+:$$BANSHEE_PLUGINS_PATH} \
    BANSHEE_DAP_PATH=$(top_builddir)/src/Banshee.Dap$${BANSHEE_DAP_PATH+:$$BANSHEE_DAP_PATH} \
	BANSHEE_PROFILES_PATH=$(top_builddir)/data/audio-profiles

