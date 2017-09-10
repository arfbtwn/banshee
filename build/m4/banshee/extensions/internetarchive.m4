AC_DEFUN([BANSHEE_CHECK_EXTENSION_INTERNETARCHIVE],
[
    AC_ARG_ENABLE(internetarchive, AC_HELP_STRING([--enable-internetarchive], [Build InternetArchive]),, enable_internetarchive="no")
    AM_CONDITIONAL(ENABLE_INTERNETARCHIVE, test "x$enable_internetarchive" = "xyes")
])
