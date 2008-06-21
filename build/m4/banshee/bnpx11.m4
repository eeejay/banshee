dnl Stolen with gratitude from Totem's configure.in

AC_DEFUN([BANSHEE_CHECK_NOW_PLAYING_X11],
[
	have_x11=no
	if test x$(pkg-config --variable=target gtk+-2.0) = xx11; then
		PKG_CHECK_MODULES(BNPX_GTK, gtk+-2.0 >= 2.8 gdk-x11-2.0 >= 2.8)

		AC_PATH_X

		have_x11=yes

		if test x"$x_includes" != x"NONE" && test -n "$x_includes" ; then
			X_INCLUDES=-I`echo $x_includes | sed -e "s/:/ -I/g"`
		fi
		if test x"$x_libraries" != x"NONE" && test -n "$x_libraries" ; then
			X_LIBRARIES=-L`echo $x_libraries | sed -e "s/:/ -L/g"`
		fi
		BNPX_CFLAGS="$X_INCLUDES $CFLAGS"
		BNPX_LIBS="$X_LIBRARIES $LIBS"
	
		PKG_CHECK_MODULES(XVIDMODE, xrandr >= 1.1.1 xxf86vm >= 1.0.1,
			have_xvidmode=yes, have_xvidmode=no)

		if test x$have_xvidmode = xyes; then
			AC_DEFINE(HAVE_XVIDMODE,, [Define this if you have the XVidMode and XRandR extension installed])
		fi

		dnl Explicit link against libX11 to avoid problems with crappy linkers
		BNPX_LIBS="$X_LIBRARIES -lX11"
		AC_SUBST(BNPX_LIBS)
		AC_SUBST(BNPX_CFLAGS)
	fi
	AM_CONDITIONAL(HAVE_XVIDMODE, [test x$have_xvidmode = xyes])
])

