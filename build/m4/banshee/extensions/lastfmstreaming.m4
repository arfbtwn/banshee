AC_DEFUN([BANSHEE_CHECK_EXTENSION_LASTFMSTREAMING],
[
    AC_ARG_ENABLE(lastfmstreaming, AC_HELP_STRING([--enable-lastfmstreaming], [Build LastfmStreaming]),, enable_lastfmstreaming="no")
    AM_CONDITIONAL(ENABLE_LASTFMSTREAMING, test "x$enable_lastfmstreaming" = "xyes")
])
