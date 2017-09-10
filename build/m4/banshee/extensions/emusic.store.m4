AC_DEFUN([BANSHEE_CHECK_EXTENSION_EMUSIC_STORE],
[
    AC_ARG_ENABLE(emusic-store, AC_HELP_STRING([--enable-emusic-store], [Build Emusic.Store]), enable_emusic_store="yes", enable_emusic_store="no")
    AM_CONDITIONAL(ENABLE_EMUSIC_STORE, test "x$enable_emusic_store" = "xyes")
])
