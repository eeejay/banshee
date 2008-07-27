AC_DEFUN([BANSHEE_CHECK_DAAP],
[
	MZC_REQUIRED=0.7.3

	AC_ARG_ENABLE(daap, AC_HELP_STRING([--disable-daap], [Disable DAAP support]), , enable_daap="yes")

	if test "x$enable_daap" = "xyes"; then
		PKG_CHECK_MODULES(MONO_ZEROCONF, mono-zeroconf >= $MZC_REQUIRED)
		AC_SUBST(MONO_ZEROCONF_LIBS)
		AM_CONDITIONAL(DAAP_ENABLED, true)
	else
		AM_CONDITIONAL(DAAP_ENABLED, false)
	fi
])
