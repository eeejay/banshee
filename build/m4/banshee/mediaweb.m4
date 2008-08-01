AC_DEFUN([BANSHEE_CHECK_MEDIAWEB],
[
	AC_ARG_ENABLE(mediaweb, AC_HELP_STRING([--enable-mediaweb], [Enable MediaWeb support - unfinished, broken]), , enable_mediaweb="no")

	if test "x$enable_mediaweb" = "xyes"; then
        PKG_CHECK_MODULES(WEBKIT, webkit-sharp-1.0 >= 0.2,
            enable_webkit=yes, enable_webkit=no)
		AC_SUBST(WEBKIT_LIBS)
		AM_CONDITIONAL(HAVE_WEBKIT, true)
	else
		AM_CONDITIONAL(HAVE_WEBKIT, false)
	fi
])

