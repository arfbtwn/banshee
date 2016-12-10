AC_DEFUN([BANSHEE_CHECK_EXTENSION_LASTFM],
[
    AC_ARG_ENABLE(lastfm, AC_HELP_STRING([--enable-lastfm], [Build Lastfm]),, enable_lastfm="no")
    AM_CONDITIONAL(ENABLE_LASTFM, test "x$enable_lastfm" = "xyes")
])
