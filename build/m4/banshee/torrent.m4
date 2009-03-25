AC_DEFUN([BANSHEE_CHECK_TORRENT],
[
	AC_ARG_ENABLE(torrent, AC_HELP_STRING([--enable-torrent], [Enable BitTorrent support - still in development]), , enable_torrent="no")

	if test "x$enable_torrent" = "xyes"; then
        PKG_CHECK_MODULES(MONOTORRENT_DBUS, monotorrent-dbus >= 0.2);
		asms="`$PKG_CONFIG --variable=Libraries monotorrent` `$PKG_CONFIG --variable=Libraries monotorrent-dbus`"
		for asm in $asms; do
			MONOTORRENT_ASSEMBLIES="$MONOTORRENT_ASSEMBLIES $asm"
		done
        AC_SUBST(MONOTORRENT_DBUS_LIBS)
		AC_SUBST(MONOTORRENT_ASSEMBLIES)
        AM_CONDITIONAL(HAVE_MONOTORRENT_DBUS, true)
	else
		AM_CONDITIONAL(HAVE_MONOTORRENT_DBUS, false)
	fi
])

