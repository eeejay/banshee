AC_DEFUN([BANSHEE_CHECK_TORRENT],
[
	AC_ARG_ENABLE(torrent, AC_HELP_STRING([--enable-torrent], [Enable BitTorrent support - still in development]), , enable_torrent="no")

	if test "x$enable_torrent" = "xyes"; then
        PKG_CHECK_MODULES(MONOTORRENT_DBUS, monotorrent-dbus >= 0.1);
        AC_SUBST(MONOTORRENT_DBUS_LIBS)
        AM_CONDITIONAL(HAVE_MONOTORRENT_DBUS, true)
	else
		AM_CONDITIONAL(HAVE_MONOTORRENT_DBUS, false)
	fi
])

