AC_DEFUN([BANSHEE_CHECK_EXTENSION_AUDIOBOOK],
[
    AC_ARG_ENABLE(audiobook, AC_HELP_STRING([--enable-audiobook], [Build Audiobook]),, enable_audiobook="no")
    AM_CONDITIONAL(ENABLE_AUDIOBOOK, test "x$enable_audiobook" = "xyes")
])
