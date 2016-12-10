AC_DEFUN([BANSHEE_CHECK_EXTENSION_NOWPLAYING],
[
    AC_ARG_ENABLE(nowplaying, AC_HELP_STRING([--enable-nowplaying], [Build NowPlaying]),, enable_nowplaying="no")
    AM_CONDITIONAL(ENABLE_NOWPLAYING, test "x$enable_nowplaying" = "xyes")
])
