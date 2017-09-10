AC_DEFUN([BANSHEE_CHECK_EXTENSION_EMUSIC],
[
    AC_ARG_ENABLE(emusic, AC_HELP_STRING([--enable-emusic], [Build Emusic]),, enable_emusic="no")
    AM_CONDITIONAL(ENABLE_EMUSIC, test "x$enable_emusic" = "xyes")
])
