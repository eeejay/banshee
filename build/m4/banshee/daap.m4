AC_DEFUN([BANSHEE_CHECK_DAAP],
[
	AC_ARG_ENABLE(daap, AC_HELP_STRING([--disable-daap], 
		[Do not build with DAAP support]), 
		enable_daap=no, enable_daap=yes)

	AC_ARG_ENABLE(mdnsd, AC_HELP_STRING([--enable-mdnsd], 
		[Use mDNSResponder instead of Avahi for DAAP]), 
		enable_mdnsd="yes", enable_mdnsd="no")
	
	AM_CONDITIONAL(ENABLE_MDNSD, test "x$enable_mdnsd" = "yes")

	AC_ARG_ENABLE(avahi, AC_HELP_STRING([--enable-avahi], 
		[Use Avahi for DAAP]), 
		enable_avahi="yes", enable_avahi="no")

	if test "x$enable_daap" = "xyes"; then 
		if test "x$enable_avahi" = "xno"; then
			AC_PATH_PROG(MDNSRESPONDER, mdnsd, no, /sbin:/usr/sbin:/usr/bin)
			if test ! "x$MDNSRESPONDER" = "xno"; then
				enable_mdnsd="yes"
			fi
		fi

		if test "x$enable_mdnsd" = "xyes"; then
			DAAPSHARP_FLAGS="-define:ENABLE_MDNSD"
			AC_SUBST(DAAPSHARP_FLAGS)
		else
			PKG_CHECK_MODULES(AVAHISHARP, avahi-sharp)
			DAAPSHARP_FLAGS=$AVAHISHARP_LIBS
			AC_SUBST(DAAPSHARP_FLAGS)
		fi
		AM_CONDITIONAL(DAAP_ENABLED, true)
	else
		AM_CONDITIONAL(DAAP_ENABLED, false)
	fi
])

