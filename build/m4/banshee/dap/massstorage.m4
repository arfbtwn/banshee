AC_DEFUN([BANSHEE_CHECK_DAP_MASS_STORAGE],
[
    AC_ARG_ENABLE(mass-storage, AC_HELP_STRING([--enable-mass-storage], [Enable mass storage DAP support]),, enable_mass_storage="yes")
    AM_CONDITIONAL(ENABLE_DAP_MASS_STORAGE, test "x$enable_mass_storage" = "xyes")
])
