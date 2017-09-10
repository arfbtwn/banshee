AC_DEFUN([BANSHEE_CHECK_EXTENSION_BOOSCRIPT],
[
    BOO_REQUIRED=0.8.1

    AC_ARG_ENABLE([booscript], [AC_HELP_STRING([--enable-booscript], [Enable Boo language support])])

    AS_CASE([$enable_booscript],
    [no],
    [
        has_boo=no
    ],
    [yes],
    [
        PKG_CHECK_MODULES(BOO, boo >= $BOO_REQUIRED, has_boo=yes)
    ],
    [
        PKG_CHECK_MODULES(BOO, boo >= $BOO_REQUIRED, has_boo=yes, has_boo=no)
        enable_booscript="auto ($has_boo)"
    ])

    AM_CONDITIONAL(ENABLE_BOOSCRIPT, test "x$has_boo" = "xyes")
])
