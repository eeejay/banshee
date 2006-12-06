AC_DEFUN([BANSHEE_CHECK_NAUTILUS_BURN],
[
	PKG_CHECK_MODULES(LIBNAUTILUS_BURN, libnautilus-burn >= 2.15, 
		lnb15=yes, lnb15=no)
	AC_MSG_RESULT([$lnb15])

	PKG_CHECK_MODULES(LIBNAUTILUS_BURN, libnautilus-burn >= 2.13, 
		lnb13=yes, lnb13=no)
	AC_MSG_RESULT([$lnb13])

	PKG_CHECK_MODULES(LIBNAUTILUS_BURN, libnautilus-burn >= 2.12, 
		lnb12=yes, lnb12=no)

	LNB_CSFLAGS=""

	if test "x$lnb15" = "xyes"; then
		LNB_SOVERSION=4
		LNB_CSFLAGS="-define:HAVE_LNB_216"
		AC_DEFINE(HAVE_LIBNAUTILUS_BURN_4, 1, 
			[Define if libnautilus-burn is version 2.15 (soversion 4) or later])
	elif test "x$lnb13" = "xyes"; then
		LNB_SOVERSION=3
	elif test "x$lnb12" = "xyes"; then
		LNB_SOVERSION=2
	else
		AC_MSG_ERROR([You need libnautilus-burn 2.12 or better])
	fi
	
	AC_SUBST(LNB_CSFLAGS)
	AC_SUBST(LNB_SOVERSION)

	AC_SUBST(LIBNAUTILUS_BURN_CFLAGS)
	AC_SUBST(LIBNAUTILUS_BURN_LIBS)
])

