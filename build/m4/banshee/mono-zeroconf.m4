AC_DEFUN([BANSHEE_CHECK_MONO_ZEROCONF],
[
	AC_ARG_ENABLE([daap], AC_HELP_STRING([--disable-daap], 
		[Do not build with DAAP support]),
		[
			if test "x$enableval" = "xno"; then
				enable_daap=no
			elif test "x$enableval" = "xyes"; then
				enable_daap=yes
			fi
		], enable_daap=yes
	)

	if test "x$enable_daap" = "xyes"; then
		PKG_CHECK_MODULES(MONO_ZEROCONF, mono-zeroconf >= 0.7.3)
		AC_SUBST(MONO_ZEROCONF_LIBS)
		AM_CONDITIONAL(DAAP_ENABLED, true)
	else
		AM_CONDITIONAL(DAAP_ENABLED, false)
	fi
])
