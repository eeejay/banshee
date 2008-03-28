dnl Automake setup

# 0.98.* = Alpha *
# 0.99.* = Beta *
AC_INIT([banshee-1], [0.98.3]) 
ASM_DISPLAY_VERSION="1.0 Alpha 3"

AC_CANONICAL_SYSTEM
AC_PREREQ(2.13)
AM_INIT_AUTOMAKE([1.9 dist-bzip2 tar-ustar])
AM_MAINTAINER_MODE

DISTCHECK_CONFIGURE_FLAGS="--disable-docs"
AC_SUBST(DISTCHECK_CONFIGURE_FLAGS)

ASM_VERSION="$VERSION.*"
AC_SUBST(ASM_VERSION)
AC_SUBST(ASM_DISPLAY_VERSION)

dnl Pieces needed by autogen; can't be parsed from macros
AM_CONFIG_HEADER(config.h)
AC_PROG_INTLTOOL([0.21])
AC_PROG_LIBTOOL
AC_PROG_INSTALL

dnl Expansions
SHAMROCK_EXPAND_LIBDIR
SHAMROCK_EXPAND_BINDIR
SHAMROCK_EXPAND_DATADIR

dnl libbanshee/native dependencies (required)
BANSHEE_CHECK_LIBBANSHEE
#BANSHEE_CHECK_NAUTILUS_BURN
BANSHEE_CHECK_GSTREAMER
SHAMROCK_CHECK_GSTREAMER_0_10_PLUGINS([
	decodebin
	playbin
	gnomevfssink
	gnomevfssrc
	audioconvert
	gconfaudiosink
])

dnl Mono and gmcs
SHAMROCK_CHECK_MONO_MODULE_NOBAIL(1.2.4)
SHAMROCK_CHECK_MONO_MODULE(1.2.2)
SHAMROCK_FIND_MONO_2_0_COMPILER
SHAMROCK_FIND_MONO_RUNTIME
SHAMROCK_CHECK_MONO_2_0_GAC_ASSEMBLIES([	
	System.Data
	System.Web
	System.Web.Services
	Mono.Cairo
	Mono.Data.SqliteClient
	Mono.Posix
	ICSharpCode.SharpZipLib
])

HAVE_MONO_1_2_4=$HAVE_MONO_MODULE
AC_SUBST(HAVE_MONO_1_2_4)

dnl NDesk DBus libraries
BANSHEE_CHECK_NDESK_DBUS

dnl Mono.Addins libraries
BANSHEE_CHECK_MONO_ADDINS

dnl notify-sharp library
BANSHEE_CHECK_NOTIFY_SHARP

dnl Monodoc (optional through --disable-docs)
SHAMROCK_CHECK_MONODOC

dnl NUnit (optional)
SHAMROCK_CHECK_MONO_NUNIT

dnl Boo libraries and compiler (optional)
#BANSHEE_CHECK_BOO

dnl External TagLib# libraries (optional)
BANSHEE_CHECK_TAGLIB_SHARP

dnl Required dependencies (managed/core)
BANSHEE_CHECK_GTK_SHARP
BANSHEE_CHECK_GNOME_SHARP

PKG_CHECK_MODULES(SQLITE, sqlite3 >= 3.4)

dnl GConf
AC_PATH_PROG(GCONFTOOL, gconftool-2, no)
AM_GCONF_SOURCE_2

dnl DAP support (each module is optional)
#BANSHEE_CHECK_DAP_IPOD
#BANSHEE_CHECK_DAP_NJB
#BANSHEE_CHECK_DAP_MTP
#BANSHEE_CHECK_DAP_KARMA

dnl DAAP support (optional)
#BANSHEE_CHECK_DAAP

dnl i18n
SHAMROCK_CONFIGURE_I18N($PACKAGE)

dnl generated files
AC_OUTPUT([
Makefile

build/Makefile
build/m4/Makefile
build/pkg-config/Makefile

data/Makefile
data/org.bansheeproject.Banshee.service
data/audio-profiles/Makefile
data/icon-theme-hicolor/Makefile

docs/Makefile
docs/Hyena/Makefile
docs/Banshee/Makefile

po/Makefile.in

tests/Makefile

extras/Makefile

libbanshee/Makefile

src/Makefile
src/AssemblyInfo.cs

src/Clients/Makefile
src/Clients/Nereid/Makefile
src/Clients/Nereid/banshee-1

src/Core/Makefile
src/Core/Banshee.Core/Makefile
src/Core/Banshee.Services/Makefile
src/Core/Banshee.ThickClient/Makefile
src/Core/Banshee.Widgets/Makefile

src/Backends/Makefile
src/Backends/Banshee.Gnome/Makefile
src/Backends/Banshee.GStreamer/Makefile
src/Backends/Banshee.Hal/Makefile
src/Backends/Banshee.Unix/Makefile

src/Libraries/Makefile
src/Libraries/Hyena/Makefile
src/Libraries/Hyena.Gui/Makefile
src/Libraries/Lastfm/Makefile
src/Libraries/Lastfm.Gui/Makefile
src/Libraries/Mono.Media/Makefile

src/Extensions/Makefile
src/Extensions/Banshee.AudioCd/Makefile
src/Extensions/Banshee.Bookmarks/Makefile
src/Extensions/Banshee.Lastfm/Makefile
src/Extensions/Banshee.MultimediaKeys/Makefile
src/Extensions/Banshee.NotificationArea/Makefile
src/Extensions/Banshee.NowPlaying/Makefile
src/Extensions/Banshee.PlayQueue/Makefile
])

if test "x${enable_daap}" = "xyes"; then
	zeroconf_lib=", Avahi"
	if test "x${enable_mdnsd}" = "xyes"; then
		zeroconf_lib=", mDNSResponder"
	fi
fi

echo "
${PACKAGE}-${VERSION}

    Install Prefix:    ${prefix}
    Datadir:           ${expanded_datadir}
    Libdir:            ${expanded_libdir}

    C Compiler:        ${CC}
    Mono C# Compiler:  ${MCS}
    Mono Runtime:      ${MONO}
    Mono >= 1.2.4:     ${HAVE_MONO_1_2_4}
    Gtk# >= 2.10:      ${HAVE_GTK_2_10}
"
#    Digital Audio Players (DAP):
#      iPod:            ${enable_ipodsharp}
#      NJB:             ${enable_njbsharp}
#      MTP:             ${enable_mtp_dap}
#      Karma:           ${enable_karmasharp}
#
#    DAAP Support:      ${enable_daap}${zeroconf_lib}
#"

if test -d ${expanded_libdir}/${PACKAGE}; then
	echo "WARNING: An existing Banshee install is in ${expanded_libdir}/${PACKAGE}"
	echo "         Remove the existing install before installing this build."
	echo "         Installing over an existing install will cause conflicts!"
	echo 
fi

