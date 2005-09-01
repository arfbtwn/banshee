/* ex: set ts=4: */
/***************************************************************************
 *  cd-rip.c
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */
 
#ifndef CD_RIP_H
#define CD_RIP_H

#include <gst/gst.h>

typedef void (* CdRipProgressCallback) (gpointer ripper, gint seconds, 
    gpointer user_info);

typedef struct {
    gchar *device;
    gint paranoia_mode;
    gint track_start;
    gint seconds;

    gchar *encoder_pipeline;
    gchar *error;
    
    gboolean cancel;
    
    GstElement *pipeline;
    GstElement *cdparanoia;
    GstElement *encoder;
    GstElement *filesink;
    
    GstFormat track_format;
    GstPad *source_pad;
    
    CdRipProgressCallback progress_callback;
} CdRip;

CdRip *cd_rip_new(gchar *device, gint paranoia_mode, gchar *encoder_pipeline);
void cd_rip_free(CdRip *ripper);
gboolean cd_rip_rip_track(CdRip *ripper, gchar *uri, gint track_number, 
    gchar *md_artist, gchar *md_album, gchar *md_title, gchar *md_genre,
    gint md_track_number, gint md_track_count, gpointer user_info);
void cd_rip_set_progress_callback(CdRip *ripper, CdRipProgressCallback cb);
void cd_rip_cancel(CdRip *ripper);
gchar *cd_rip_get_error(CdRip *ripper);

#endif /* CD_RIP_H */

