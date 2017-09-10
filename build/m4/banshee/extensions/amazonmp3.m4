AC_DEFUN([BANSHEE_CHECK_EXTENSION_AMAZONMP3],
[
    AC_ARG_ENABLE(amazonmp3, AC_HELP_STRING([--enable-amazonmp3], [Build AmazonMp3]),, enable_amazonmp3="no")
    AM_CONDITIONAL(ENABLE_AMAZONMP3, test "x$enable_amazonmp3" = "xyes")
])
