AC_DEFUN([BANSHEE_CHECK_EXTENSION_UBUNTUONE],
[
    UBUNTUONESHARP_REQUIRED=0.9.2

    AC_ARG_ENABLE(ubuntuone, AC_HELP_STRING([--enable-ubuntuone], [Build Ubuntu One Store Extension]))

    AS_CASE([$enable_ubuntuone],
    [no],
    [
        has_ubuntuonesharp=no
    ],
    [yes],
    [
        PKG_CHECK_MODULES(UBUNTUONESHARP, [ubuntuone-sharp-1.0 >= $UBUNTUONESHARP_REQUIRED], has_ubuntuonesharp=yes)
    ],
    [
        PKG_CHECK_MODULES(UBUNTUONESHARP, [ubuntuone-sharp-1.0 >= $UBUNTUONESHARP_REQUIRED], has_ubuntuonesharp=yes, has_ubuntuonesharp=no)
        enable_ubuntuone="auto ($has_ubuntuonesharp)"
    ])

    AM_CONDITIONAL(ENABLE_UBUNTUONE, test "x$enable_ubuntuone" = "xyes")
])
