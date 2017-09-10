AC_DEFUN([BANSHEE_CHECK_EXTENSION_BPM],
[
    AC_ARG_ENABLE(bpm, AC_HELP_STRING([--enable-bpm], [Build Bpm]),, enable_bpm="no")
    AM_CONDITIONAL(ENABLE_BPM, test "x$enable_bpm" = "xyes")
])
