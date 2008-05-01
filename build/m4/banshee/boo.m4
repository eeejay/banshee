AC_DEFUN([BANSHEE_CHECK_BOO],
[
	PKG_CHECK_MODULES(BOO, boo >= 0.7.6, enable_boo="yes", enable_boo="no")
	AC_SUBST(BOO_LIBS)
])

