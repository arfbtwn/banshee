AC_DEFUN([BANSHEE_CHECK_EXTENSION_INTERNETRADIO],
[
    AC_ARG_ENABLE(internetradio, AC_HELP_STRING([--enable-internetradio], [Build InternetRadio]),, enable_internetradio="no")
    AM_CONDITIONAL(ENABLE_INTERNETRADIO, test "x$enable_internetradio" = "xyes")
])
