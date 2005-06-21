AC_INIT(src/Main.cs)

AM_CONFIG_HEADER(config.h)

AC_CANONICAL_SYSTEM
AC_PREREQ(2.13)
AM_INIT_AUTOMAKE(sonance, 0.4.0)
AM_MAINTAINER_MODE

AC_PROG_INSTALL
AC_PROG_CC
AC_ISC_POSIX
AC_HEADER_STDC
AM_PROG_LIBTOOL

dnl Bundled gst-sharp
bundledGstSharp="no"
AC_ARG_ENABLE(bundled-gst-sharp,
[  --enable-bundled-gst-sharp      
                          Install Bundled gst-sharp Assembly],
bundledGstSharp="yes" )
AC_SUBST(bundledGstSharp)

AM_CONDITIONAL(INSTALL_GST_SHARP, test "x$bundledGstSharp" = "xyes")

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
	AC_MSG_ERROR([You need to install pkg-config])
fi

MONO_REQUIRED_VERSION=1.0.0
PKG_CHECK_MODULES(MONO, mono >= $MONO_REQUIRED_VERSION)

MONODIR=`$PKG_CONFIG --variable=exec_prefix mono`
MONOBIN="$MONODIR/bin"
MONO="$MONOBIN/mono"
MINT="$MONOBIN/mint"
MCS="$MONOBIN/mcs"
GACUTIL="$MONOBIN/gacutil"

if test ! -x "$MONO" -a ! -x "$MINT"; then
	AC_MSG_ERROR([No Mono runtime found])
fi

if test ! -x "$MONO"; then
    MONO = $MINT
fi

CS="C#"
if test ! -x "$MCS"; then
	AC_MSG_ERROR([No $CS compiler found])
fi

if test ! -x "$GACUTIL"; then
	AC_MSG_ERROR([No Mono gacutil found])
fi

AC_SUBST(MCS)
AC_SUBST(MONO)
AC_SUBST(GACUTIL)

GTKSHARP_REQUIRED=1.9
PKG_CHECK_MODULES(GTKSHARP,
	gtk-sharp-2.0 >= $GTKSHARP_REQUIRED \
	gnome-sharp-2.0 >= $GTKSHARP_REQUIRED \
	gnome-vfs-sharp-2.0 >= $GTKSHARP_REQUIRED \
	glade-sharp-2.0 >= $GTKSHARP_REQUIRED \
	gconf-sharp-2.0 >= $GTKSHARP_REQUIRED \
	gapi-2.0 >= $GTKSHARP_REQUIRED)
AC_SUBST(GTKSHARP_LIBS)

PKG_CHECK_MODULES(DBUSSHARP, dbus-sharp)
AC_SUBST(DBUSSHARP)

if test "x$bundledGstSharp" = "xno"; then
GSTSHARP_REQUIRED=0.2.4
PKG_CHECK_MODULES(GSTSHARP, gst-sharp >= $GSTSHARP_REQUIRED)
AC_SUBST(GSTSHARP_LIBS)
else
GSTSHARP_LIBS="-r:./gst-sharp.dll"
GSTSHARP_BUNDLED_ASM="gst-sharp.dll"
AC_SUBST(GSTSHARP_BUNDLED_ASM)
AC_SUBST(GSTSHARP_LIBS)
fi

SQLITE_REQUIRED=1.8.15
PKG_CHECK_MODULES(SQLITE, sqlite >= $SQLITE_REQUIRED)

## probably should make gstreamer-plugins optional
## but generating conditionally the source is not really possible
GSTREAMER_REQUIRED_VERSION=0.8.8
GSTREAMER_PLUGINS_REQUIRED_VERSION=0.8.7
AC_SUBST(GSTREAMER_REQUIRED_VERSION)
AC_SUBST(GSTREAMER_PLUGINS_REQUIRED_VERSION)
PKG_CHECK_MODULES(GST_DEPENDENCIES,
gstreamer >= $GSTREAMER_REQUIRED_VERSION
gstreamer-gconf >= $GSTREAMER_PLUGINS_REQUIRED_VERSION
gstreamer-interfaces >= $GSTREAMER_PLUGINS_REQUIRED_VERSION
gstreamer-play >= $GSTREAMER_PLUGINS_REQUIRED_VERSION
gstreamer-plugins >= $GSTREAMER_PLUGINS_REQUIRED_VERSION,
enable_gst=yes, enable_gst=no 
)

if test "x$enable_gst" = "xno" ; then
  PKG_CHECK_MODULES(GST_DEPENDENCIES,
  gstreamer-0.8 >= $GSTREAMER_REQUIRED_VERSION
  gstreamer-gconf-0.8 >= $GSTREAMER_PLUGINS_REQUIRED_VERSION
  gstreamer-interfaces-0.8 >= $GSTREAMER_PLUGINS_REQUIRED_VERSION
  gstreamer-play-0.8 >= $GSTREAMER_PLUGINS_REQUIRED_VERSION
  gstreamer-plugins-0.8 >= $GSTREAMER_PLUGINS_REQUIRED_VERSION,
  enable_gst=yes, enable_gst=no
  )

  if test "x$enable_gst" = "xno" ; then
	AC_MSG_ERROR([Install gstreamer and gstreamer-plugins])
  fi
fi


dnl C Stuff

GTK_REQUIRED=2.4
PKG_CHECK_MODULES(SONANCE,
	gconf-2.0 \
	gdk-pixbuf-2.0 \
	gnome-vfs-2.0 \
	gnome-desktop-2.0 \
	gtk+-2.0 >= $GTK_REQUIRED)
AC_SUBST(SONANCE_CFLAGS)
AC_SUBST(SONANCE_LIBS)

dnl Muine Metadata Stuff

dnl Check for Ogg Vorbis
MM_PATH_OGG(, AC_MSG_ERROR(You need Ogg))
AC_SUBST(OGG_CFLAGS)
AC_SUBST(OGG_LIBS)

MM_PATH_VORBIS(, AC_MSG_ERROR(You need Vorbis))
AC_SUBST(VORBIS_CFLAGS)
AC_SUBST(VORBIS_LIBS)
AC_SUBST(VORBISFILE_CFLAGS)
AC_SUBST(VORBISFILE_LIBS)

dnl Check for FLAC
AC_CHECK_HEADER(FLAC/all.h, FLAC_LIBS="-lFLAC", AC_MSG_ERROR(You need FLAC))
AC_SUBST(FLAC_LIBS)

dnl Check for libid3tag
AC_ARG_ENABLE(id3tag, [  --disable-id3tag        Disable MP3 support],,have_id3tag=yes)
if test "x$have_id3tag" = "xyes"; then
	PKG_CHECK_MODULES(ID3TAG, id3tag >= 0.15, id3tag_detect=yes, id3tag_detect=no)
	if test "x$id3tag_detect" = "xno"; then
		AC_CHECK_HEADER(id3tag.h, id3tag_detect=yes, id3tag_detect=no)
	fi
	if test "x$id3tag_detect" = "xyes"; then
		ID3TAG_LIBS="-lid3tag -lz"
		AC_DEFINE(HAVE_ID3TAG, 1, id3tag support)
	else
		AC_DEFINE(HAVE_ID3TAG, 0, id3tag support)
		id3tag_detect = "no"
	fi
else
	AC_DEFINE(HAVE_ID3TAG, 0, id3tag support)
	id3tag_detect = "no"
fi

AM_CONDITIONAL(HAVE_ID3TAG, test "x$id3tag_detect" = "xyes")
AC_SUBST(ID3TAG_LIBS)

dnl Check for FAAD 
AC_ARG_ENABLE(faad2, [  --disable-faad2         Disable AAC support],,enable_faad2=yes)
if test "x$enable_faad2" = "xyes"; then
	AC_CHECK_HEADER(mp4ff.h, faad2_detect=yes, faad_detect=no)
	if test "x$faad2_detect" = "xyes"; then
		FAAD_LIBS="-lmp4ff -lfaad"
		AC_DEFINE(HAVE_FAAD, 1, faad2 support)
	else
		AC_DEFINE(HAVE_FAAD, 0, faad2 support)
		faad2_detect = "no"
	fi
else
	AC_DEFINE(HAVE_FAAD, 0, faad2 support)
	faad2_detect = "no"
fi

AM_CONDITIONAL(HAVE_FAAD, test "x$faad2_detect" = "xyes")
AC_SUBST(FAAD_LIBS)

AC_OUTPUT([
Makefile
sonance.pc
src/Makefile
src/AssemblyInfo.cs
src/sonance
src/sonance.exe.config
libsonance/Makefile
libsonance/egg/Makefile
libsonance/id3-vfs/Makefile
m4/Makefile
data/Makefile
data/sonance.desktop
data/glade/Makefile
data/images/Makefile
])

echo "
sonance-$VERSION

	Install Prefix:   ${prefix}
	C Compiler:       ${CC}
	Mono C# Compiler: ${MCS}
	Mono Runtime:     ${MONO}
	GACUTIL:          ${GACUTIL}

	Have ID3 Tag:     ${id3tag_detect}
	Have FAAD:        ${faad2_detect}

	Install Bundled gst-sharp: ${bundledGstSharp}

"
