AC_DEFUN([BANSHEE_CHECK_GSTREAMER],
[
    GSTREAMER_REQUIRED_VERSION=1.0.0
    GSTREAMER_SHARP_REQUIRED_VERSION=0.99.0

    AC_ARG_ENABLE(gst, AC_HELP_STRING([--enable-gst=native], [Enable GStreamer backend]),, enable_gst=native)

    AS_CASE([$enable_gst],
    [native],
    [
        BANSHEE_CHECK_LIBBANSHEE

        PKG_CHECK_MODULES(GST,
            gstreamer-1.0 >= $GSTREAMER_REQUIRED_VERSION
            gstreamer-base-1.0 >= $GSTREAMER_REQUIRED_VERSION
            gstreamer-controller-1.0 >= $GSTREAMER_REQUIRED_VERSION
            gstreamer-plugins-base-1.0 >= $GSTREAMER_REQUIRED_VERSION
            gstreamer-audio-1.0 >= $GSTREAMER_REQUIRED_VERSION
            gstreamer-fft-1.0 >= $GSTREAMER_REQUIRED_VERSION
            gstreamer-pbutils-1.0 >= $GSTREAMER_REQUIRED_VERSION
            gstreamer-tag-1.0 >= $GSTREAMER_REQUIRED_VERSION
            gstreamer-video-1.0 >= $GSTREAMER_REQUIRED_VERSION)

        AC_SUBST(GST_CFLAGS)
        AC_SUBST(GST_LIBS)

        enable_gst_native=yes
        enable_gst_sharp=no
    ],
    [managed],
    [
        PKG_CHECK_MODULES(GST_SHARP, gstreamer-sharp-1.0 >= $GSTREAMER_SHARP_REQUIRED_VERSION)
        AC_SUBST(GST_SHARP_LIBS)

        enable_gst_native=no
        enable_gst_sharp=yes
    ],
    [
        AC_MSG_ERROR([Please pass --enable-gst=native|managed])
    ])

    AM_CONDITIONAL(ENABLE_GST_NATIVE, test "x$enable_gst_native" = "xyes")
    AM_CONDITIONAL(ENABLE_GST_SHARP, test "x$enable_gst_sharp" = "xyes")
])
