AC_DEFUN([BANSHEE_CHECK_GNOME],
[
    GNOMESHARP_REQUIRED=2.8

    AC_ARG_ENABLE(gnome, AC_HELP_STRING([--enable-gnome], [Enable GNOME support]))

    AS_CASE([$enable_gnome],
    [no],
    [
        AM_CONDITIONAL(GCONF_SCHEMAS_INSTALL, false)
        AM_CONDITIONAL(ENABLE_GNOME, false)
    ],
    [yes],
    [
        PKG_CHECK_MODULES(GCONFSHARP, gconf-sharp-2.0 >= $GNOMESHARP_REQUIRED)
        AC_SUBST(GCONFSHARP_LIBS)

        AC_PATH_PROG(GCONFTOOL, gconftool-2, no)

        # libgconf check needed because its -devel pkg should contain AM_GCONF_SOURCE_2 macro, see bgo#604416
        PKG_CHECK_MODULES(LIBGCONF, gconf-2.0)

        # needed so autoconf doesn't complain before checking the existence of libgconf2-devel above
        m4_pattern_allow([AM_GCONF_SOURCE_2])

        AM_GCONF_SOURCE_2

        # dbus-glib is needed for the workaround for bgo#692374
        PKG_CHECK_MODULES(DBUS_GLIB, dbus-glib-1 >= 0.80, have_dbus_glib="yes", have_dbus_glib="no")
        if test "x$have_dbus_glib" = "xno"; then
            AC_MSG_ERROR([Please install dbus-glib-1 development package or use --disable-gnome.])
        fi

        AM_CONDITIONAL(ENABLE_GNOME, true)
    ],
    [
        has_gnome="no"
        PKG_CHECK_MODULES(GCONFSHARP, gconf-sharp-2.0 >= $GNOMESHARP_REQUIRED,
        [
            AC_SUBST(GCONFSHARP_LIBS)

            AC_PATH_PROG(GCONFTOOL, gconftool-2, no)

            # libgconf check needed because its -devel pkg should contain AM_GCONF_SOURCE_2 macro, see bgo#604416
            PKG_CHECK_MODULES(LIBGCONF, gconf-2.0,
            [

                # needed so autoconf doesn't complain before checking the existence of libgconf2-devel above
                m4_pattern_allow([AM_GCONF_SOURCE_2])

                AM_GCONF_SOURCE_2

                # dbus-glib is needed for the workaround for bgo#692374
                PKG_CHECK_MODULES(DBUS_GLIB, dbus-glib-1 >= 0.80, have_dbus_glib="yes", have_dbus_glib="no")

                AM_CONDITIONAL(ENABLE_GNOME, test "x$have_dbus_glib" = "xyes")
                has_gnome=$have_dbus_glib

            ], [AM_CONDITIONAL(ENABLE_GNOME, false)])

        ], [AM_CONDITIONAL(ENABLE_GNOME, false)])

        enable_gnome="auto ($has_gnome)"
    ])
])
