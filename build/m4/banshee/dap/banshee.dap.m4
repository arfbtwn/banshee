AC_DEFUN([BANSHEE_CHECK_DAP],
[
    AC_ARG_ENABLE(dap, AC_HELP_STRING([--enable-dap], [Enable DAP support]),, enable_dap="no")
    AM_CONDITIONAL(ENABLE_DAP, test "x$enable_dap" = "xyes")
])
