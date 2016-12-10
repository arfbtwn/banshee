AC_DEFUN([BANSHEE_CHECK_GIO_SHARP],
[
    GIOSHARP_REQUIRED=2.99
    GUDEVSHARP_REQUIRED=3.0

    AC_ARG_ENABLE(gio, AC_HELP_STRING([--enable-gio], [Enable GIO for IO operations]))
    AC_ARG_ENABLE(gio_hardware, AC_HELP_STRING([--enable-gio-hardware], [Enable GIO Hardware backend]))

    AS_CASE([$enable_gio],
    [no],
    [
        has_gio_sharp=no
    ],
    [yes],
    [
        PKG_CHECK_MODULES(GIOSHARP, gio-sharp-3.0 >= $GIOSHARP_REQUIRED, has_gio_sharp=yes)
    ],
    [
        PKG_CHECK_MODULES(GIOSHARP, gio-sharp-3.0 >= $GIOSHARP_REQUIRED, has_gio_sharp=yes, has_gio_sharp=no)
        enable_gio="auto ($has_gio_sharp)"
    ])

    AS_CASE([$enable_gio_hardware],
    [no],
    [
        has_gudev_sharp=no
    ],
    [yes],
    [
        PKG_CHECK_MODULES(GUDEV_SHARP, gudev-sharp-3.0 >= $GUDEVSHARP_REQUIRED, has_gudev_sharp=yes)
    ],
    [
        PKG_CHECK_MODULES(GUDEV_SHARP, gudev-sharp-3.0 >= $GUDEVSHARP_REQUIRED, has_gudev_sharp=yes, has_gudev_sharp=no)
        enable_gio_hardware="auto ($has_gudev_sharp)"
    ])

    AM_CONDITIONAL(ENABLE_GIO, test "x$has_gio_sharp" = "xyes")
    AM_CONDITIONAL(ENABLE_GIO_HARDWARE, test "x$has_gudev_sharp" = "xyes")
])

