AC_INIT(src/Main.cs)

AM_CONFIG_HEADER(config.h)

AC_CANONICAL_SYSTEM
AC_PREREQ(2.13)

VERSION_MAJOR=0
VERSION_MINOR=9
VERSION_MICRO=12
VERSION_REVIS=no

VERSION="$VERSION_MAJOR.$VERSION_MINOR.$VERSION_MICRO"
ASM_VERSION="$VERSION.*"

if test ! "x$VERSION_REVIS" = "xno" ; then	
	VERSION="$VERSION.$VERSION_REVIS"
	ASM_VERSION=$VERSION
fi

AM_INIT_AUTOMAKE(banshee, $VERSION)
AC_SUBST(ASM_VERSION)

AM_MAINTAINER_MODE

AC_PROG_INTLTOOL([0.21])

AC_PROG_INSTALL
AC_ISC_POSIX
AM_PROG_LIBTOOL
AC_PROG_CC
AC_PROG_CXX

dnl check headers
AC_HEADER_STDC

GNOME_COMPILE_WARNINGS
AM_PATH_GLIB_2_0

dnl configure options
AC_ARG_ENABLE(helix, AC_HELP_STRING([--enable-helix], [Enable Helix/RealPlayer engine]), , enable_helix="yes")
AM_CONDITIONAL(ENABLE_HELIX, test "x$enable_helix" = "xyes")

AC_ARG_WITH(helix-libs, AC_HELP_STRING([--with-helix-libs], [Specify Helix/RealPlayer Library/Codecs Path]), helix_libs="$withval", helix_libs="no")

AC_ARG_ENABLE(vlc, AC_HELP_STRING([--enable-vlc], [Enable VLC Media Engine]), , enable_vlc="no")
AM_CONDITIONAL(ENABLE_VLC, test "x$enable_vlc" = "xyes")

AC_ARG_ENABLE(xing, AC_HELP_STRING([--enable-xing], [Enable Real's Xing MP3 Encoder]), , enable_xing="no")
AM_CONDITIONAL(ENABLE_XING, test "x$enable_xing" = "xyes")

AC_ARG_ENABLE(dev-tests, AC_HELP_STRING([--enable-dev-tests], [Whether or not to build various test programs for components in libbanshee and mediaengines]), , enable_dev_tests="no")
AM_CONDITIONAL(ENABLE_TESTS, test "x$enable_dev_tests" = "xyes")

AC_ARG_WITH(internal-mcs, AC_HELP_STRING([--with-internal-mcs], [Use the internal mcs 1.1.9.2 compiler]), internal_mcs="yes", internal_mcs="no")

AC_ARG_WITH(gstreamer-dev, AC_HELP_STRING([--with-gstreamer-dev], [Build the GStreamer 0.9 backends (DEVELOPMENTAL)]), enable_gst_dev="yes", enable_gst_dev="no")
AM_CONDITIONAL(ENABLE_GST_DEV, test "x$enable_gst_dev" = "xyes")

if test "x$enable_xing" = "xyes"; then
	AC_DEFINE(ENABLE_XING, 1, [Define if the Xing Mp3 encoder should be compiled and linked into libbanshee])
fi

dnl pkg-config, install programs
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
	AC_MSG_ERROR([You need to install pkg-config])
fi

AC_PATH_PROG(INSTALL, install, no)
if test "x$INSTALL" = "xno"; then
	AC_MSG_ERROR([You need to install 'install'])
fi

AC_SUBST(INSTALL)
AC_SUBST(PKG_CONFIG)

if test "x$internal_mcs" = "xno"; then
	MONO_REQUIRED_VERSION=1.1.9
	PKG_CHECK_MODULES(MONO, mono >= $MONO_REQUIRED_VERSION)
fi

dnl Mono programs
MONODIR=`$PKG_CONFIG --variable=exec_prefix mono`
MONOBIN="$MONODIR/bin"
MONO="$MONOBIN/mono"

if test "x$internal_mcs" = "xyes"; then
	MCS="$MONO `pwd`/src/mcs.exe"
else
	MCS="$MONOBIN/mcs"
fi

AC_MSG_CHECKING([for mono.pc])
if test -z `$PKG_CONFIG --variable=prefix mono`; then
	AC_MSG_ERROR([missing the mono.pc file, usually found in the mono-devel package])
else
	AC_MSG_RESULT([found])
fi

required_assemblies="Mono.Data.SqliteClient Mono.Posix System.Runtime.Remoting System.Web System.Web.Services"

for asm in $required_assemblies; do
	AC_MSG_CHECKING([for $asm.dll])
	if test ! -e `$PKG_CONFIG --variable=prefix mono`/lib/mono/1.0/$asm.dll; then
		AC_MSG_ERROR([missing required Mono DLL: $asm.dll])
	else
		AC_MSG_RESULT([found])
	fi
done

if test ! -x "$MONO"; then
	AC_MSG_ERROR([No Mono runtime found])
fi

if test "x$internal_mcs" = "xno"; then
	CS="C#"
	if test ! -x "$MCS"; then
		AC_MSG_ERROR([No $CS compiler found])
	fi
fi

AC_SUBST(MCS)
AC_SUBST(MONO)

dnl gtk-sharp, other CLR deps
GTKSHARP_REQUIRED=2.3.92
PKG_CHECK_MODULES(GTKSHARP,
	gtk-sharp-2.0 >= $GTKSHARP_REQUIRED \
	gnome-sharp-2.0 >= $GTKSHARP_REQUIRED \
	gnome-vfs-sharp-2.0 >= $GTKSHARP_REQUIRED \
	glade-sharp-2.0 >= $GTKSHARP_REQUIRED \
	gconf-sharp-2.0 >= $GTKSHARP_REQUIRED)
AC_SUBST(GTKSHARP_LIBS)

IPODSHARP_REQUIRED=0.5.11
PKG_CHECK_MODULES(IPODSHARP, 
	ipod-sharp >= $IPODSHARP_REQUIRED \
	ipod-sharp-ui >= $IPODSHARP_REQUIRED)	
AC_SUBST(IPODSHARP_LIBS)

PKG_CHECK_MODULES(DBUSSHARP, dbus-sharp)
AC_SUBST(DBUSSHARP)

dnl sqlite devel dep
PKG_CHECK_MODULES(SQLITE, sqlite3)

dnl musicbrainz
PKG_CHECK_MODULES(MUSICBRAINZ, libmusicbrainz >= 2.1.1)

dnl gstreamer devel deps

if test "x$enable_gst_dev" = "xyes"; then
	GSTREAMER_REQUIRED_VERSION=0.9.5
	AC_SUBST(GSTREAMER_REQUIRED_VERSION)
	PKG_CHECK_MODULES(GST,
		gstreamer-0.9 >= $GSTREAMER_REQUIRED_VERSION
		gstreamer-base-0.9 >= $GSTREAMER_REQUIRED_VERSION
		gstreamer-plugins-base-0.9 >= $GSTREAMER_REQUIRED_VERSION
		gstreamer-controller-0.9 >= $GSTREAMER_REQUIRED_VERSION
		gstreamer-dataprotocol-0.9 >= $GSTREAMER_REQUIRED_VERSION)

	if test "x$enable_xing" = "xyes"; then
		AC_MSG_ERROR([The Xing GStreamer plugin has not been ported to GStreamer 0.9 and cannot be enabled if you enable GStreamer 0.9])
	fi
else
	GSTREAMER_REQUIRED_VERSION=0.8.11
	GSTREAMER_PLUGINS_REQUIRED_VERSION=0.8.11
	AC_SUBST(GSTREAMER_REQUIRED_VERSION)
	AC_SUBST(GSTREAMER_PLUGINS_REQUIRED_VERSION)

	gst_pkg_name="gstreamer"

	PKG_CHECK_MODULES(GST,
		gstreamer >= $GSTREAMER_REQUIRED_VERSION
		gstreamer-gconf >= $GSTREAMER_PLUGINS_REQUIRED_VERSION
		gstreamer-interfaces >= $GSTREAMER_PLUGINS_REQUIRED_VERSION
		gstreamer-play >= $GSTREAMER_PLUGINS_REQUIRED_VERSION
		gstreamer-plugins >= $GSTREAMER_PLUGINS_REQUIRED_VERSION,
		enable_gst=yes, enable_gst=no)

	if test "x$enable_gst" = "xno" ; then
		PKG_CHECK_MODULES(GST,
			gstreamer-0.8 >= $GSTREAMER_REQUIRED_VERSION
			gstreamer-gconf-0.8 >= $GSTREAMER_PLUGINS_REQUIRED_VERSION
			gstreamer-interfaces-0.8 >= $GSTREAMER_PLUGINS_REQUIRED_VERSION
			gstreamer-play-0.8 >= $GSTREAMER_PLUGINS_REQUIRED_VERSION
			gstreamer-plugins-0.8 >= $GSTREAMER_PLUGINS_REQUIRED_VERSION,
			enable_gst=yes, enable_gst=no)

		gst_pkg_name="gstreamer-0.8"

		if test "x$enable_gst" = "xno" ; then
			AC_MSG_ERROR([Install gstreamer, gstreamer-plugins gstreamer-gconf, gstreamer-interfaces, and gstreamer-play])
		fi
	fi
fi

AC_SUBST(GST_CFLAGS)
AC_SUBST(GST_LIBS)

GST_PLUGINS_DIR="`$PKG_CONFIG --variable=libdir $gst_pkg_name`/$gst_pkg_name"
if test ! -d "$GST_PLUGINS_DIR"; then
    AC_MSG_ERROR([GStreamer plugins directory could not be found])
fi

AC_SUBST(GST_PLUGINS_DIR)

dnl GTK+-2.0 C devel deps
GTK_REQUIRED=2.6
PKG_CHECK_MODULES(GTK,
	glib-2.0 \
	libgnome-2.0 \
	libgnomeui-2.0 \
	libbonobo-2.0 \
	gconf-2.0 \
	gdk-pixbuf-2.0 \
	gnome-vfs-2.0 \
	gnome-desktop-2.0 \
	gtk+-2.0 >= $GTK_REQUIRED)
AC_SUBST(GTK_CFLAGS)
AC_SUBST(GTK_LIBS)

dnl HAL devel deps
PKG_CHECK_MODULES(HAL,
	gobject-2.0 \
	glib-2.0 \
	dbus-1 \
	dbus-glib-1 \
	hal >= 0.5.2)
AC_SUBST(HAL_CFLAGS)
AC_SUBST(HAL_LIBS)

dnl libnautilus-burn devel deps
PKG_CHECK_MODULES(LIBNAUTILUS_BURN, libnautilus-burn >= 2.13, lnb13=yes, lnb13=no)
if test "x$lnb13" = "xyes"; then
	LNB_SOVERSION=3
else
	PKG_CHECK_MODULES(LIBNAUTILUS_BURN, libnautilus-burn >= 2.12, lnb12=yes, lnb12=no)
	if test "x$lnb12" = "xyes"; then
		LNB_SOVERSION=2
	else
		AC_MSG_ERROR([You need libnautilus-burn 2.12 or better])
	fi
fi
AC_SUBST(LNB_SOVERSION)
AC_SUBST(LIBNAUTILUS_BURN_CFLAGS)
AC_SUBST(LIBNAUTILUS_BURN_LIBS)

dnl GConf

AC_PATH_PROG(GCONFTOOL, gconftool-2, no)
AM_GCONF_SOURCE_2

dnl HELIX_LIBS stuff

if test "x$enable_helix" = "xyes"; then
	POSSIBLE_HELIX_LIBS="/usr/lib/RealPlayer10 /usr/lib/RealPlayer /usr/lib/realplayer /usr/local/realplayer /usr/local/RealPlayer10 /usr/local/RealPlayer /opt/realplayer /usr/lib/realplay-10.0.5 /usr/lib/realplay-10.0.4 /usr/lib/realplay-10.0.3 /usr/lib/realplay-10.0.2 /usr/lib/realplay-10.0.1 /usr/lib/realplay-10.0.0 /usr/lib/realplay-10.0 /usr/lib/helix /usr/local/Helix /usr/local/HelixPlayer /usr/lib/helix/player /usr/local/helix/player /usr/local/lib/helix/player"
	COMMON_HELIX_SIGNATURE="common/clntcore.so"

	if test ! -e $helix_libs/$COMMON_HELIX_SIGNATURE; then
		helix_libs="no";
		for possible in $POSSIBLE_HELIX_LIBS; do
			if test -e $possible/$COMMON_HELIX_SIGNATURE ; then
				helix_libs="$possible";
				break;
			fi
		done
	fi

	if test "x$helix_libs" = "xyes"; then
		AC_MSG_ERROR([		
You have enabled the Helix Player Engine, but configure could not 
find a RealPlayer or Helix Player installation. 
		
Please configure with --with-helix-libs=/path/to/install or to 
disable the Helix engine, configure with --disable-helix])
	else
		HELIX_LIBRARY_PATH="$helix_libs";
		AC_SUBST(HELIX_LIBRARY_PATH)
	fi
fi

dnl Find the real @libdir@

expanded_libdir=`( case $prefix in NONE) prefix=$ac_default_prefix ;; *) ;; esac
		   case $exec_prefix in NONE) exec_prefix=$prefix ;; *) ;; esac
		   eval echo $libdir )`
AC_SUBST(expanded_libdir)

expanded_datadir=`( case $prefix in NONE) prefix=$ac_default_prefix ;; *) ;; esac
		   case $exec_prefix in NONE) exec_prefix=$prefix ;; *) ;; esac
		   eval echo $datadir )`
AC_SUBST(expanded_datadir)

dnl Defines for C Exec Wrapper (${prefix}/bin/banshee)

AC_DEFINE_UNQUOTED([PREFIX], ["${prefix}"], [install prefix])
AC_DEFINE_UNQUOTED([LIBDIR], ["${expanded_libdir}"], [libdir])
AC_DEFINE_UNQUOTED([MONO], ["${MONO}"], [Mono Runtime])
AC_DEFINE_UNQUOTED([MONO_PATH], ["${MONO_PATH}"], [MONO_PATH Env])
AC_DEFINE_UNQUOTED([HELIX_LIBRARY_PATH], ["${HELIX_LIBRARY_PATH}"], [Helix Library Path])

dnl Unstable assembly installation

UNSTABLE_INSTALL_ASSEMBLIES="ipod-sharp ipod-sharp-ui"
INSTALLED_ASSEMBLIES="`$PKG_CONFIG --variable=Libraries $UNSTABLE_INSTALL_ASSEMBLIES`"
AC_SUBST(INSTALLED_ASSEMBLIES)

dnl Intl

ALL_LINGUAS="bg ca en_CA es lt nb nl pt_BR sv zh_CN"
GETTEXT_PACKAGE=banshee
AC_SUBST(GETTEXT_PACKAGE)
AC_DEFINE_UNQUOTED(GETTEXT_PACKAGE, "$GETTEXT_PACKAGE", [Gettext Package])
AM_GLIB_GNU_GETTEXT

dnl generated files
AC_OUTPUT([
Makefile
banshee.pc
entagged-sharp/Makefile
hal-sharp/Makefile
hal-sharp/hal-sharp.dll.config
libbanshee/Makefile
libbanshee/xing/Makefile
burn-sharp/Makefile
burn-sharp/burn-sharp.dll.config
src/banshee
src/Makefile
src/AssemblyInfo.cs
src/banshee.exe.config
src/ipod-sharp.dll.config
src/MusicBrainz/Makefile
src/MusicBrainz/MusicBrainz.dll.config
src/GtkSharpBackports/Makefile
src/GtkSharpBackports/GtkSharpBackports.dll.config
src/Banshee.MediaEngine/Makefile
src/Banshee.MediaEngine/Gstreamer/Makefile
src/Banshee.MediaEngine/Gstreamer/gst-player.dll.config
src/Banshee.MediaEngine/Helix/Makefile
src/Banshee.MediaEngine/Helix/helix-sharp.dll.config
src/Banshee.MediaEngine/Vlc/Makefile
src/Banshee.MediaEngine/Vlc/vlc-player.dll.config
src/Banshee.Widgets/Makefile
src/Banshee.Widgets/Banshee.Widgets.dll.config
src/Banshee.Base/Makefile
src/Banshee.Base/Banshee.Base.dll.config
src/Banshee.Base/ConfigureDefines.cs
src/Banshee.Dap/Makefile
src/Banshee.Dap/Ipod/Makefile
src/Banshee.Dap/Njb/Makefile
data/Makefile
data/org.gnome.Banshee.service
data/glade/Makefile
data/images/Makefile
data/images/ipod/Makefile
po/Makefile.in
])

gst_version="0.8";
if test "x${enable_gst_dev}" = "xyes"; then
	gst_version="0.9";
fi;

echo "
banshee-$VERSION

    Install Prefix:    ${prefix}
    C Compiler:        ${CC}
    Mono C# Compiler:  ${MCS}
    Mono Runtime:      ${MONO}

    Engines:
      GStreamer:       yes, ${gst_version}
      Helix:           ${enable_helix}
      VLC:             ${enable_vlc}

    Xing MP3 Encoder:  ${enable_xing}
    GStreamer Plugins: ${GST_PLUGINS_DIR}
"

if test "x${enable_helix}" = "xyes"; then
	echo "    Helix/RealPlayer:  ${helix_libs}
	";
fi;

if test "x${enable_gst_dev}" = "xyes"; then
	echo "";
	echo "    ** Warning: GStreamer 0.9 backend may be very unstable **";
	echo "";
fi;

if test "x${enable_vlc}" = "xyes"; then
	if test ! -e ./src/Banshee.MediaEngine/Vlc/libvlc.so; then
		echo ""
		echo "    *****************************************************"
		echo "    NOTICE: You have enabled the VLC engine, but have not"
		echo "    provided a libvlc.so in ./src/Banshee.MediaEngine/Vlc/"
		echo ""
		echo "    You must build your own libvlc.so library and copy it"
		echo "    to ./src/Banshee.MediaEngine/Vlc/libvlc.so before running"
		echo "    'make install' or copy libvlc.so after the install to"
		echo "    ${expanded_libdir}/banshee/src/Banshee.MediaEngine/Vlc/libvlc.so"
		echo ""
		echo "    libvlc.so is not included in Banshee."
		echo ""
	fi;
fi;

