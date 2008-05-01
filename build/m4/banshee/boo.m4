AC_DEFUN([BANSHEE_CHECK_BOO],
[
	PKG_CHECK_MODULES(BOO, boo >= 0.8.1, enable_boo="yes", enable_boo="no")
	AC_SUBST(BOO_LIBS)
	AM_CONDITIONAL(HAVE_BOO,  test "x$enable_boo" = "xyes")
])

