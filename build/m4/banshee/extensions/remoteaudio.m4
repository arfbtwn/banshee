AC_DEFUN([BANSHEE_CHECK_EXTENSION_REMOTEAUDIO],
[
    AC_ARG_ENABLE(remoteaudio, AC_HELP_STRING([--enable-remoteaudio], [Build RemoteAudio]),, enable_remoteaudio="$has_daap")

    AS_CASE([$enable_remoteaudio,$has_daap],
    [yes,no],
    [
        AC_MSG_ERROR(Please build --with-daap)
    ])

    AM_CONDITIONAL(ENABLE_REMOTEAUDIO, test "x$enable_remoteaudio" = "xyes")
])
