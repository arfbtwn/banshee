//
// banshee-gst.c
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#ifdef HAVE_CONFIG_H
#  include "config.h"
#endif

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <stdarg.h>

#include <gst/gst.h>
#include <gst/pbutils/pbutils.h>

#include "banshee-gst.h"
#include "banshee-player-private.h"

static gboolean gstreamer_initialized = FALSE;
static gboolean banshee_debugging;
static BansheeLogHandler banshee_log_handler = NULL;
static gint banshee_version = -1;

MYEXPORT void
gstreamer_initialize (gboolean debugging, BansheeLogHandler log_handler)
{
    if (gstreamer_initialized) {
        return;
    }
    
    banshee_debugging = debugging;
    banshee_log_handler = log_handler;

    gst_init (NULL, NULL);
    
    gst_pb_utils_init ();
    
    gstreamer_initialized = TRUE;
}

MYEXPORT void
gstreamer_deinitialize ()
{
    if (!gstreamer_initialized) {
        return;
    }

    bp_debug ("Deinitializing GStreamer");
    gst_deinit();

    gstreamer_initialized = FALSE;
}

MYEXPORT gboolean 
gstreamer_test_pipeline (gchar *pipeline)
{
    GstElement *element = NULL;
    GError *error = NULL;
    
    element = gst_parse_launch (pipeline, &error);

    if (element != NULL) {
        gst_object_unref (GST_OBJECT (element));
    }
    
    return error == NULL;
}

MYEXPORT gchar *
gstreamer_version_string ()
{
    return g_strdup_printf ("%i.%i.%i.%i", GST_VERSION_MAJOR, GST_VERSION_MINOR, GST_VERSION_MICRO, GST_VERSION_NANO);
}

gboolean
banshee_is_debugging ()
{
    return banshee_debugging;
}

guint
banshee_get_version_number ()
{
    guint16 major = 0, minor = 0, micro = 0;
    
    if (banshee_version >= 0) {
        return (guint)banshee_version;
    }
    
    if (sscanf (VERSION, "%" G_GUINT16_FORMAT ".%" G_GUINT16_FORMAT ".%" G_GUINT16_FORMAT, 
        &major, &minor, &micro) == 3) {
        banshee_version = ((guint8)major << 16) | ((guint8)minor << 8) | ((guint8)micro);
        // major = (banshee_version >> 16)
        // minor = (banshee_version >> 8) & 0x00FF
        // micro = banshee_version & 0x0000FF
    } else {
         banshee_version = 0;
    }   
    
    return (guint)banshee_version;
}

static void
banshee_log (BansheeLogType type, const gchar *component, const gchar *message)
{
    if (banshee_log_handler == NULL) {
        switch (type) {
            case BANSHEE_LOG_TYPE_WARNING: g_warning ("%s: %s", component, message); break;
            case BANSHEE_LOG_TYPE_ERROR:   g_error ("%s: %s", component, message); break;
            default:                       g_debug ("%s: %s", component, message); break;
        }
        return;
    }
    
    (banshee_log_handler) (type, component, message);
}

void
banshee_log_debug (const gchar *component, const gchar *format, ...)
{
    va_list args;
    gchar *message;
    
    if (!banshee_debugging) {
        return;
    }
    
    va_start (args, format);
    message = g_strdup_vprintf (format, args);
    va_end (args);
    
    banshee_log (BANSHEE_LOG_TYPE_DEBUG, component, message);
    
    g_free (message);
}
