AC_DEFUN([BANSHEE_CHECK_BOO],
[
	AC_ARG_ENABLE(external_boo, AC_HELP_STRING([--enable-external-boo], 
		[Use external Boo instead of the bundled one]), 
		enable_external_boo="yes", enable_external_boo="no")

	if test "x$enable_external_boo" = "xyes"; then
		PKG_CHECK_MODULES(BOO, boo >= 0.7.6)
		AC_SUBST(BOO_LIBS)
		AM_CONDITIONAL(EXTERNAL_BOO, true)
	else
		AM_CONDITIONAL(EXTERNAL_BOO, false)
	fi

	AC_PATH_PROG(BOOC, booc, no)
	AM_CONDITIONAL(HAVE_BOOC, test ! "x$BOOC" = "xno")

	AC_SUBST(BOOC)
])

