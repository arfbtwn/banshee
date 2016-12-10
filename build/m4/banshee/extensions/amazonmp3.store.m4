AC_DEFUN([BANSHEE_CHECK_EXTENSION_AMAZONMP3_STORE],
[
    AC_ARG_ENABLE(amazonmp3-store, AC_HELP_STRING([--enable-amazonmp3-store], [Build AmazonMp3.Store]), enable_amazonmp3_store="yes", enable_amazonmp3_store="no")
    AM_CONDITIONAL(ENABLE_AMAZONMP3_STORE, test "x$enable_amazonmp3_store" = "xyes")
])
