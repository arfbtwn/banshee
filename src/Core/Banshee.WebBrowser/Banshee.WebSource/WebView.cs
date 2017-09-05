//
// WebView.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright 2010 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Runtime.InteropServices;
using Gtk;

using Hyena;
using Hyena.Downloader;

using Banshee.Base;
using Banshee.IO;
using Banshee.WebBrowser;

namespace Banshee.WebSource
{
    public abstract class WebView : OssiferWebView
    {
        protected string FixupJavascriptUrl { get; set; }
        private string fixup_javascript;
        private bool fixup_javascript_fetched;

        public bool IsReady { get; private set; }
        public bool CanSearch { get; protected set; }

        public event EventHandler Ready;

        public WebView ()
        {
            CanSearch = false;
        }

        const float ZOOM_STEP = 0.05f;
        private double smooth_scroll_size;

        public void ZoomIn ()
        {
            Zoom += ZOOM_STEP;
        }

        public void ZoomOut ()
        {
            Zoom -= ZOOM_STEP;
        }

        [DllImport ("libgdk-3-0.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gdk_event_get_scroll_deltas (IntPtr eventHandle, out double deltaX, out double deltaY);

        protected override bool OnScrollEvent (Gdk.EventScroll scroll)
        {
            if ((scroll.State & Gdk.ModifierType.ControlMask) != 0) {
                switch (scroll.Direction) {
                    case Gdk.ScrollDirection.Up:
                        ZoomIn ();
                        break;
                    case Gdk.ScrollDirection.Down:
                        ZoomOut ();
                        break;
                    case (Gdk.ScrollDirection)4: //GDK_SCROLL_SMOOTH
                        double delta_x;
                        double delta_y;
                        gdk_event_get_scroll_deltas (scroll.Handle, out delta_x, out  delta_y);
                        smooth_scroll_size += delta_y;

                        if (smooth_scroll_size < -1) {
                            ZoomIn ();
                            smooth_scroll_size += 1;
                        } else if (smooth_scroll_size > 1) {
                            ZoomOut ();
                            smooth_scroll_size -= 1;
                        }
                        break;
                    default:
                        return true;
                }
                return true;
            }
            return base.OnScrollEvent (scroll);
        }

        protected override void OnLoadStatusChanged (OssiferLoadStatus status)
        {
            if ((status == OssiferLoadStatus.FirstVisuallyNonEmptyLayout ||
                status == OssiferLoadStatus.Finished) && Uri != "about:blank") {
                if (fixup_javascript != null) {
                    ExecuteScript (fixup_javascript);
                }
            }

            base.OnLoadStatusChanged (status);
        }

        protected override OssiferNavigationResponse OnMimeTypePolicyDecisionRequested (string mimetype)
        {
            // We only explicitly accept (render) text/html -- everything else is ignored.
            switch (mimetype) {
                case "text/html": return OssiferNavigationResponse.Accept;
                default:
                    Log.Debug ("OssiferWebView: ignoring mime type", mimetype);
                    return OssiferNavigationResponse.Ignore;
            }
        }

        public abstract void GoHome ();

        public virtual void GoSearch (string query)
        {
        }

        public void FullReload ()
        {
            // This is an HTML5 Canvas/JS spinner icon. It is awesome
            // and renders immediately, going away when the store loads.
            LoadString (AssemblyResource.GetFileContents ("loading.html"),
                "text/html", "UTF-8", null);

            // Here we download and save for later injection some JavaScript
            // to fix-up the Amazon pages. We don't store this locally since
            // it may need to be updated if Amazon's page structure changes.
            // We're mainly concerned about hiding the "You don't have Flash"
            // messages, since we do the streaming of previews natively.
            if (FixupJavascriptUrl != null && !fixup_javascript_fetched) {
                fixup_javascript_fetched = true;
                new Hyena.Downloader.HttpStringDownloader () {
                    Uri = new Uri (FixupJavascriptUrl),
                    Finished = (d) => {
                        if (d.State.Success) {
                            fixup_javascript = d.Content;
                        }
                        LoadHome ();
                    },
                    AcceptContentTypes = new [] { "text/javascript" }
                }.Start ();
            } else {
                LoadHome ();
            }
        }

        private void LoadHome ()
        {
            // We defer this to another main loop iteration, otherwise
            // our load placeholder document will never be rendered.
            GLib.Idle.Add (delegate {
                GoHome ();

                // Emit the Ready event once we are first allowed
                // to load the home page (ensures we've downloaded
                // the fixup javascript, etc.).
                if (!IsReady) {
                    IsReady = true;
                    var handler = Ready;
                    if (handler != null) {
                        handler (this, EventArgs.Empty);
                    }
                }
                return false;
            });
        }
    }
}
