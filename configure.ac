AC_INIT(src/Main.cs)

AM_CONFIG_HEADER(config.h)

AC_CANONICAL_SYSTEM
AC_PREREQ(2.13)
AM_INIT_AUTOMAKE(banshee, 0.8.4)
AM_MAINTAINER_MODE

AC_PROG_INSTALL
AC_ISC_POSIX
AC_HEADER_STDC
AM_PROG_LIBTOOL

dnl configure options
AC_ARG_ENABLE(helix, AC_HELP_STRING([--enable-helix], [Enable Helix/RealPlayer engine]), , enable_helix="no")
AM_CONDITIONAL(ENABLE_HELIX, test "x$enable_helix" = "xyes")

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

MONO_REQUIRED_VERSION=1.1.8
PKG_CHECK_MODULES(MONO, mono >= $MONO_REQUIRED_VERSION)

dnl Mono programs
MONODIR=`$PKG_CONFIG --variable=exec_prefix mono`
MONOBIN="$MONODIR/bin"
MONO="$MONOBIN/mono"
MCS="$MONOBIN/mcs"

if test ! -x "$MONO" -a ; then
	AC_MSG_ERROR([No Mono runtime found])
fi

CS="C#"
if test ! -x "$MCS"; then
	AC_MSG_ERROR([No $CS compiler found])
fi

AC_SUBST(MCS)
AC_SUBST(MONO)

dnl gtk-sharp, other CLR deps
GTKSHARP_REQUIRED=1.9
PKG_CHECK_MODULES(GTKSHARP,
	gtk-sharp-2.0 >= $GTKSHARP_REQUIRED \
	gnome-sharp-2.0 >= $GTKSHARP_REQUIRED \
	gnome-vfs-sharp-2.0 >= $GTKSHARP_REQUIRED \
	glade-sharp-2.0 >= $GTKSHARP_REQUIRED \
	gconf-sharp-2.0 >= $GTKSHARP_REQUIRED)
AC_SUBST(GTKSHARP_LIBS)

PKG_CHECK_MODULES(IPODSHARP, ipod-sharp >= 0.4.9)
AC_SUBST(IPODSHARP_LIBS)

PKG_CHECK_MODULES(DBUSSHARP, dbus-sharp)
AC_SUBST(DBUSSHARP)

dnl sqlite devel dep
PKG_CHECK_MODULES(SQLITE, sqlite >= 2.8)

dnl sqlite runtime dep
AC_PATH_PROG(SQLITE, sqlite, no)
if test "x$SQLITE" = "xno"; then
	AC_MSG_ERROR([You need sqlite 2.8.x])
fi

dnl gstreamer devel deps
GSTREAMER_REQUIRED_VERSION=0.8.8
GSTREAMER_PLUGINS_REQUIRED_VERSION=0.8.7
AC_SUBST(GSTREAMER_REQUIRED_VERSION)
AC_SUBST(GSTREAMER_PLUGINS_REQUIRED_VERSION)

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

	if test "x$enable_gst" = "xno" ; then
		AC_MSG_ERROR([Install gstreamer and gstreamer-plugins])
	fi
fi

AC_SUBST(GST_CFLAGS)
AC_SUBST(GST_LIBS)

dnl GTK+-2.0 C devel deps
GTK_REQUIRED=2.6
PKG_CHECK_MODULES(BANSHEE,
	gconf-2.0 \
	gdk-pixbuf-2.0 \
	gnome-vfs-2.0 \
	gnome-desktop-2.0 \
	gtk+-2.0 >= $GTK_REQUIRED)
AC_SUBST(BANSHEE_CFLAGS)
AC_SUBST(BANSHEE_LIBS)

dnl libnautilus-burn devel deps
PKG_CHECK_MODULES(LIBNAUTILUS_BURN, libnautilus-burn >= 2.10)
AC_SUBST(LIBNAUTILUS_BURN_CFLAGS)
AC_SUBST(LIBNAUTILUS_BURN_LIBS)

dnl generated files
AC_OUTPUT([
Makefile
banshee.pc
entagged/Makefile
hal-sharp/Makefile
mediaengines/Makefile
mediaengines/gst/Makefile
mediaengines/helix/Makefile
mediaengines/helix/include/Makefile
burn-sharp/Makefile
plugincore/Makefile
src/Makefile
src/AssemblyInfo.cs
src/ConfigureDefines.cs
src/banshee
src/banshee.exe.config
libsonance/Makefile
data/Makefile
data/banshee.desktop
data/glade/Makefile
data/images/Makefile
])

echo "
banshee-$VERSION

	Install Prefix:   ${prefix}
	C Compiler:       ${CC}
	Mono C# Compiler: ${MCS}
	Mono Runtime:     ${MONO}

	Engines:
	   GStreamer:     yes
	   Helix:         ${enable_helix}
"
