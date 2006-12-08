dnl Automake setup
AC_INIT(src/Main.cs)
AC_CANONICAL_SYSTEM
AC_PREREQ(2.13)

VERSION=0.11.3
ASM_VERSION="$VERSION.*"
AC_SUBST(ASM_VERSION)

AM_INIT_AUTOMAKE(banshee, $VERSION)
AM_MAINTAINER_MODE

DISTCHECK_CONFIGURE_FLAGS="--disable-docs"
AC_SUBST(DISTCHECK_CONFIGURE_FLAGS)

dnl Pieces needed by gnome-autogen; can't be parsed from macros
AM_CONFIG_HEADER(config.h)
AC_PROG_INTLTOOL([0.21])
AC_PROG_LIBTOOL
AC_PROG_INSTALL
GNOME_COMPILE_WARNINGS

dnl libbanshee/native dependencies (required)
BANSHEE_CHECK_LIBBANSHEE
BANSHEE_CHECK_NAUTILUS_BURN
BANSHEE_CHECK_GSTREAMER
SHAMROCK_CHECK_GSTREAMER_0_10_PLUGINS([
	decodebin
	playbin
	cdparanoia
	gnomevfssink
	gnomevfssrc
	audioconvert
	gconfaudiosink
])

dnl GConf
AC_PATH_PROG(GCONFTOOL, gconftool-2, no)
AM_GCONF_SOURCE_2

dnl Mono and gmcs
SHAMROCK_CHECK_MONO_MODULE(1.1.10)
SHAMROCK_FIND_MONO_2_0_COMPILER
SHAMROCK_FIND_MONO_RUNTIME
SHAMROCK_CHECK_MONO_2_0_GAC_ASSEMBLIES([	
	System.Data
	System.Web
	System.Web.Services
	Mono.Cairo
	Mono.Data.SqliteClient
	Mono.Posix
])
BANSHEE_VERIFY_GMCS

dnl Monodoc (optional through --disable-docs)
SHAMROCK_CHECK_MONODOC

dnl NUnit (optional)
SHAMROCK_CHECK_MONO_NUNIT

dnl Boo libraries and compiler (optional)
BANSHEE_CHECK_BOO

dnl Required dependencies (managed/core)
BANSHEE_CHECK_GTK_SHARP
PKG_CHECK_MODULES(SQLITE, sqlite3 >= 3.2)

dnl DAP support (each module is optional)
BANSHEE_CHECK_DAP_IPOD
BANSHEE_CHECK_DAP_NJB
BANSHEE_CHECK_DAP_MTP
BANSHEE_CHECK_DAP_KARMA

dnl DAAP support (optional)
BANSHEE_CHECK_DAAP

dnl Helix playback engine (optional)
BANSHEE_CHECK_HELIX_ENGINE

dnl Expansions
SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

dnl i18n
SHAMROCK_CONFIGURE_I18N(banshee)

dnl generated files
AC_OUTPUT([
Makefile
banshee.pc
hal-sharp/Makefile
taglib-sharp/Makefile
dbus-sharp/Makefile
dbus-sharp/NDesk.DBus.GLib.dll.config
libbanshee/Makefile
branding/Makefile
build/Makefile
build/m4/Makefile
src/banshee
src/Makefile
src/AssemblyInfo.cs
src/banshee.exe.config
src/MusicBrainz/Makefile
src/MusicBrainz/MusicBrainz.dll.config
src/Gnome.Keyring/Makefile
src/Last.FM/Makefile
src/Banshee.MediaEngine/Makefile
src/Banshee.MediaEngine/Gstreamer/Makefile
src/Banshee.MediaEngine/Helix/Makefile
src/Banshee.Widgets/Makefile
src/Banshee.Widgets/Banshee.Widgets.dll.config
src/Banshee.Base/Makefile
src/Banshee.Base/Banshee.Base.dll.config
src/Banshee.Base/ConfigureDefines.cs
src/Banshee.Dap/Makefile
src/Banshee.Dap/Ipod/Makefile
src/Banshee.Dap/Ipod/ipod-sharp.dll.config
src/Banshee.Dap/Karma/Makefile
src/Banshee.Dap/MassStorage/Makefile
src/Banshee.Dap/Mtp/Makefile
src/Banshee.Dap/Njb/Makefile
src/Banshee.Dap/Njb/njb-sharp.dll.config
src/Banshee.Plugins/Makefile
src/Banshee.Plugins/Audioscrobbler/Makefile
src/Banshee.Plugins/Audioscrobbler/Banshee.Plugins.Audioscrobbler.dll.config
src/Banshee.Plugins/MetadataSearch/Makefile
src/Banshee.Plugins/MetadataSearch/MetadataSearch.dll.config
src/Banshee.Plugins/MMKeys/Makefile
src/Banshee.Plugins/MMKeys/MMKeys.dll.config
src/Banshee.Plugins/Daap/Makefile
src/Banshee.Plugins/Daap/Daap.dll.config
src/Banshee.Plugins/NotificationAreaIcon/Makefile
src/Banshee.Plugins/NotificationAreaIcon/NotificationAreaIcon.dll.config
src/Banshee.Hyena/Makefile
src/Boo/Makefile
src/BooBuddy/Makefile
data/Makefile
data/org.gnome.Banshee.service
data/images/Makefile
docs/Makefile
tests/Makefile
tools/Makefile
tools/banshee-import
po/Makefile.in
])

if test "x${enable_daap}" = "xyes"; then
	zeroconf_lib=", Avahi"
	if test "x${enable_mdnsd}" = "xyes"; then
		zeroconf_lib=", mDNSResponder"
	fi
fi

echo "
banshee-$VERSION

    Install Prefix:    ${prefix}
    Datadir:           ${expanded_datadir}
    Libdir:            ${expanded_libdir}

    C Compiler:        ${CC}
    Mono C# Compiler:  ${MCS}
    Mono Runtime:      ${MONO}

    Engines:
      GStreamer:       yes
      Helix Remote:    ${enable_helix}

    Digital Audio Players (DAP):
      iPod:            ${enable_ipodsharp}
      NJB:             ${enable_njbsharp}
      MTP:             ${enable_mtp_dap}
      Karma:           ${enable_karmasharp}

    DAAP Support:      ${enable_daap}${zeroconf_lib}
"

