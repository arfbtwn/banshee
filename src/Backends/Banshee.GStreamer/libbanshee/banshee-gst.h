//
// banshee-gst.h
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

#ifndef _BANSHEE_GST_H
#define _BANSHEE_GST_H

#include <glib.h>

#ifdef WIN32
#define MYEXPORT __declspec(dllexport)
#else
#define MYEXPORT
#endif


#define BANSHEE_GST_ITERATOR_ITERATE(iter,child_type,child_name,free,block) { \
    gboolean iter##_done = FALSE; \
    while (!iter##_done) { \
        GValue child_value = {0, }; \
        child_type child_name; \
        switch (gst_iterator_next (iter, &child_value)) { \
            case GST_ITERATOR_OK: { \
                child_name = (child_type) g_value_get_object(&child_value); \
                { block; } \
                break; \
            } \
            default: iter##_done = TRUE; break; \
        } \
    } \
    if (free) gst_iterator_free (iter); \
}

typedef enum {
    BANSHEE_LOG_TYPE_DEBUG,
    BANSHEE_LOG_TYPE_WARNING,
    BANSHEE_LOG_TYPE_INFORMATION,
    BANSHEE_LOG_TYPE_ERROR
} BansheeLogType;

typedef void (* BansheeLogHandler) (BansheeLogType type, const gchar *component, const gchar *message);

MYEXPORT void
gstreamer_initialize (gboolean debugging, BansheeLogHandler log_handler);
void      gstreamer_deinitialize ();
gboolean  banshee_is_debugging ();
guint     banshee_get_version_number ();

void      banshee_log_debug (const gchar *component, const gchar *format, ...);

#endif /* _BANSHEE_GST_H */
