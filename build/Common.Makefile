## This is a project-wide Makefile helper
## Do not use $(MCS), $(MCS_FLAGS), $(GTKSHARP_LIBS), etc
## directly; please use the variables in this file as they
## will make maintaining the build system much easier

## Directories ##

DIR_HAL = $(top_builddir)/ext/hal-sharp
DIR_TAGLIB = $(top_builddir)/ext/taglib-sharp
DIR_DBUS = $(top_builddir)/ext/dbus-sharp
DIR_MONO_ADDINS = $(top_builddir)/ext/mono-addins
DIR_LAST_FM = $(top_builddir)/src/Extras/Last.FM
DIR_MUSICBRAINZ = $(top_builddir)/src/Extras/MusicBrainz
DIR_GNOME_KEYRING = $(top_builddir)/src/Extras/Gnome.Keyring
DIR_HYENA = $(top_builddir)/src/Core/Hyena
DIR_HYENA_GUI = $(top_builddir)/src/Core/Hyena.Gui
DIR_BANSHEE_CORE = $(top_builddir)/src/Core/Banshee.Core
DIR_BANSHEE_SERVICES = $(top_builddir)/src/Core/Banshee.Services
DIR_BANSHEE_WIDGETS = $(top_builddir)/src/Core/Banshee.Widgets
DIR_BANSHEE_THICKCLIENT = $(top_builddir)/src/Core/Banshee.ThickClient
DIR_BANSHEE_BASE = $(top_builddir)/src/Core/Banshee.Base
DIR_BOO = $(top_srcdir)/src/Extras/Boo
DIR_BOOBUDDY = $(top_builddir)/src/Extras/BooBuddy

DIR_DAP = $(top_builddir)/src/Dap
RUN_DIR_DAP_IPOD = $(DIR_DAP)/Banshee.Dap.Ipod:$$(dirname `pkg-config --variable=Libraries ipod-sharp`)
RUN_DIR_DAP_NJB = $(DIR_DAP)/Banshee.Dap.Njb:$$(dirname `pkg-config --variable=Libraries njb-sharp`):$$(pkg-config --variable=libdir njb-sharp)/njb-sharp
RUN_DIR_DAP_MTP = $(DIR_DAP)/Banshee.Dap.Mtp:$$(dirname `pkg-config --variable=Libraries libgphoto2-sharp`)
RUN_DIR_DAP_MASS_STORAGE = $(DIR_DAP)/Banshee.Dap.MassStorage
RUN_DIR_DAP_ALL=$(RUN_DIR_DAP_IPOD):$(RUN_DIR_DAP_NJB):$(RUN_DIR_DAP_MTP):$(RUN_DIR_DAP_MASS_STORAGE)

## Linking ##

LINK_GLIB = $(GLIBSHARP_LIBS)
LINK_GTK = $(GTKSHARP_LIBS)
LINK_CAIRO = -r:Mono.Cairo
LINK_MONO_UNIX = -r:Mono.Posix

LINK_SQLITE = -r:System.Data -r:Mono.Data.SqliteClient
LINK_HAL = -r:$(DIR_HAL)/Hal.dll
LINK_TAGLIB = -r:$(DIR_TAGLIB)/TagLib.dll
LINK_MONO_ADDINS_CORE = -r:$(DIR_MONO_ADDINS)/Mono.Addins/Mono.Addins.dll
LINK_LAST_FM = -r:$(DIR_LAST_FM)/Last.FM.dll
LINK_MUSICBRAINZ = -r:$(DIR_MUSICBRAINZ)/MusicBrainz.dll
LINK_GNOME_KEYRING = -r:$(DIR_GNOME_KEYRING)/Gnome.Keyring.dll

LINK_DBUS = $(NDESK_DBUS_LIBS)

if EXTERNAL_BOO
LINK_BOO = $(BOO_LIBS)
else
LINK_BOO = \
	-r:$(DIR_BOO)/Boo.Lang.dll \
	-r:$(DIR_BOO)/Boo.Lang.Compiler.dll \
	-r:$(DIR_BOO)/Boo.Lang.Interpreter.dll
endif
LINK_BOOBUDDY = -r:$(DIR_BOOBUDDY)/BooBuddy.dll

LINK_HYENA = -r:$(DIR_HYENA)/Hyena.dll
LINK_HYENA_GUI = -r:$(DIR_HYENA_GUI)/Hyena.Gui.dll
LINK_BANSHEE_CORE_ASM = -r:$(DIR_BANSHEE_CORE)/Banshee.Core.dll
LINK_BANSHEE_SERVICES = -r:$(DIR_BANSHEE_SERVICES)/Banshee.Services.dll
LINK_BANSHEE_THICKCLIENT = -r:$(DIR_BANSHEE_THICKCLIENT)/Banshee.ThickClient.dll
LINK_BANSHEE_BASE = -r:$(DIR_BANSHEE_BASE)/Banshee.Base.dll
LINK_BANSHEE_WIDGETS = -r:$(DIR_BANSHEE_WIDGETS)/Banshee.Widgets.dll
LINK_BANSHEE_CORE = $(LINK_HYENA) $(LINK_HYENA_GUI) $(LINK_BANSHEE_CORE_ASM) $(LINK_BANSHEE_SERVICES) $(LINK_BANSHEE_BASE) $(LINK_BANSHEE_THICKCLIENT) $(LINK_BANSHEE_WIDGETS) $(LINK_TAGLIB)

## Building ##

# Ignoring 0278 due to a bug in gmcs: 
# http://bugzilla.ximian.com/show_bug.cgi?id=79998
BUILD_FLAGS = -debug -nowarn:0278
BUILD = $(MCS) $(BUILD_FLAGS)
BUILD_LIB = $(BUILD) -target:library

BUILD_BANSHEE_CORE = \
	MONO_PATH=$(DIR_DBUS) \
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

MONO_BASE_PATH = $(DIR_HAL):$(DIR_TAGLIB):$(DIR_DBUS):$(DIR_MONO_ADDINS)/Mono.Addins:$(DIR_LAST_FM):$(DIR_MUSICBRAINZ):$(DIR_GNOME_KEYRING):$(DIR_HYENA):$(DIR_HYENA_GUI):$(DIR_BANSHEE_CORE):$(DIR_BANSHEE_SERVICES):$(DIR_BANSHEE_WIDGETS):$(DIR_BANSHEE_THICKCLIENT):$(DIR_BANSHEE_BASE):$(DIR_BOO):$(DIR_BOOBUDDY)

RUN_PATH = \
	LD_LIBRARY_PATH=$(top_builddir)/libbanshee/.libs:$(RUN_DIR_DAP_NJB) \
	DYLD_LIBRARY_PATH=$${LD_LIBRARY_PATH} \
	MONO_PATH=$(MONO_BASE_PATH):$(RUN_DIR_DAP_ALL)${MONO_PATH+:$$MONO_PATH} \
	BANSHEE_ENGINES_PATH=$(top_builddir)/src/Engines \
	BANSHEE_PLUGINS_PATH=$(top_builddir)/src/Plugins$${BANSHEE_PLUGINS_PATH+:$$BANSHEE_PLUGINS_PATH} \
    BANSHEE_DAP_PATH=$(top_builddir)/src/Dap$${BANSHEE_DAP_PATH+:$$BANSHEE_DAP_PATH} \
	BANSHEE_PROFILES_PATH=$(top_builddir)/data/audio-profiles

