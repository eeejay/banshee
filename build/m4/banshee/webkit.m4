AC_DEFUN([BANSHEE_CHECK_WEBKIT],
[
	AC_ARG_ENABLE(webkit, AC_HELP_STRING([--enable-webkit], [Enable experimental Wikipedia and MediaWeb extensions- unfinished, likely broken]), , enable_webkit="no")

	if test "x$enable_webkit" = "xyes"; then
        PKG_CHECK_MODULES(WEBKIT, webkit-sharp-1.0 >= 0.2,
            enable_webkit=yes, enable_webkit=no)
		AC_SUBST(WEBKIT_LIBS)
		AM_CONDITIONAL(HAVE_WEBKIT, true)
	else
		AM_CONDITIONAL(HAVE_WEBKIT, false)
	fi
])

