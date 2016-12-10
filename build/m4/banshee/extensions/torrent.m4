AC_DEFUN([BANSHEE_CHECK_EXTENSION_TORRENT],
[
    MTD_VERSION=0.2

    AC_ARG_ENABLE(torrent, AC_HELP_STRING([--enable-torrent], [Enable BitTorrent support - still in development]))

    AS_CASE([$enable_torrent],
    [no],
    [
        has_monotorrent_dbus=no
    ],
    [yes],
    [
        PKG_CHECK_MODULES(MONOTORRENT_DBUS, monotorrent-dbus >= $MTD_VERSION, has_monotorrent_dbus=yes)
    ],
    [
        PKG_CHECK_MODULES(MONOTORRENT_DBUS, monotorrent-dbus >= $MTD_VERSION, has_monotorrent_dbus=yes, has_monotorrent_dbus=no)
        enable_torrent="auto ($has_monotorrent_dbus)"
    ])

    AM_CONDITIONAL(ENABLE_TORRENT, test "x$has_monotorrent_dbus" = "xyes")

    AS_IF(test "x$has_monotorrent_dbus" = "xyes",
    [
        asms="`$PKG_CONFIG --variable=Libraries monotorrent` `$PKG_CONFIG --variable=Libraries monotorrent-dbus`"
        for asm in $asms; do
            MONOTORRENT_ASSEMBLIES="$MONOTORRENT_ASSEMBLIES $asm"
        done
        AC_SUBST(MONOTORRENT_DBUS_LIBS)
        AC_SUBST(MONOTORRENT_ASSEMBLIES)
    ])
])
