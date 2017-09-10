AC_DEFUN([BANSHEE_CHECK_EXTENSION_OPTICALDISC],
[
    AC_ARG_ENABLE(opticaldisc, AC_HELP_STRING([--enable-opticaldisc], [Build OpticalDisc]),, enable_opticaldisc="no")
    AM_CONDITIONAL(ENABLE_OPTICALDISC, test "x$enable_opticaldisc" = "xyes")
])
