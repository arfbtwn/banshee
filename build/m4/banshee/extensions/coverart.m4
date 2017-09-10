AC_DEFUN([BANSHEE_CHECK_EXTENSION_COVERART],
[
    AC_ARG_ENABLE(coverart, AC_HELP_STRING([--enable-coverart], [Build CoverArt]),, enable_coverart="no")
    AM_CONDITIONAL(ENABLE_COVERART, test "x$enable_coverart" = "xyes")
])
