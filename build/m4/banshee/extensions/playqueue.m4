AC_DEFUN([BANSHEE_CHECK_EXTENSION_PLAYQUEUE],
[
    AC_ARG_ENABLE(playqueue, AC_HELP_STRING([--enable-playqueue], [Build PlayQueue]),, enable_playqueue="no")
    AM_CONDITIONAL(ENABLE_PLAYQUEUE, test "x$enable_playqueue" = "xyes")
])
