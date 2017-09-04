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


using Gdk;
using GLib;

using Hyena.Downloader;

using Banshee.Base;
using Banshee.WebBrowser;

using EventArgs = System.EventArgs;
using EventHandler = System.EventHandler;
using Log = Hyena.Log;

namespace Banshee.WebSource
{
    public abstract class WebView : OssiferWebView
    {
        private const float ZoomStep = 0.05f;

        [DllImport ("libgdk-3-0.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gdk_event_get_scroll_deltas (IntPtr eventHandle, out double deltaX, out double deltaY);

        private string fixup_javascript;
        private bool fixup_javascript_fetched;
        private double smooth_scroll_size;

        public WebView ()
        {
            CanSearch = false;
        }

        protected string FixupJavascriptUrl { get; set; }

        public bool IsReady { get; private set; }
        public bool CanSearch { get; protected set; }

        public event EventHandler Ready;

        public void ZoomIn ()
        {
            Zoom += ZoomStep;
        }

        public void ZoomOut ()
        {
            Zoom -= ZoomStep;
        }

        protected override bool OnScrollEvent (EventScroll scroll)
        {
            if ((scroll.State & ModifierType.ControlMask) != 0) {
                switch (scroll.Direction) {
                    case ScrollDirection.Up:
                        ZoomIn ();
                        break;
                    case ScrollDirection.Down:
                        ZoomOut ();
                        break;
                    case (ScrollDirection) 4: // GDK_SCROLL_SMOOTH
                        double delta_x;
                        double delta_y;
                        gdk_event_get_scroll_deltas (scroll.Handle, out delta_x, out delta_y);
                        smooth_scroll_size += delta_y;

                        if (smooth_scroll_size < -1) {
                            ZoomIn ();
                            smooth_scroll_size += 1;
                        }
                        else if (smooth_scroll_size > 1) {
                            ZoomOut ();
                            smooth_scroll_size -= 1;
                        }
                        break;
                    default:
                        return base.OnScrollEvent (scroll);
                }
                return true;
            }
            return base.OnScrollEvent (scroll);
        }

        protected override void OnLoadStatusChanged (OssiferLoadStatus status)
        {
            if (Uri != "about:blank" && fixup_javascript != null) {
                switch (status) {
                    case OssiferLoadStatus.FirstVisuallyNonEmptyLayout:
                    case OssiferLoadStatus.Finished:
                        ExecuteScript (fixup_javascript);
                        break;
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
            LoadHtml (AssemblyResource.GetFileContents ("loading.html"), null);

            // Here we download and save for later injection some JavaScript
            // to fix-up the Amazon pages. We don't store this locally since
            // it may need to be updated if Amazon's page structure changes.
            // We're mainly concerned about hiding the "You don't have Flash"
            // messages, since we do the streaming of previews natively.
            if (FixupJavascriptUrl != null && !fixup_javascript_fetched) {
                fixup_javascript_fetched = true;
                new HttpStringDownloader
                {
                    Uri = new Uri (FixupJavascriptUrl),
                    Finished = d => {
                        if (d.State.Success) {
                            fixup_javascript = d.Content;
                        }
                        LoadHome ();
                    },
                    AcceptContentTypes = new[] {"text/javascript"}
                }.Start ();
            }
            else {
                LoadHome ();
            }
        }

        private void LoadHome ()
        {
            // We defer this to another main loop iteration, otherwise
            // our load placeholder document will never be rendered.
            Idle.Add (delegate {
                GoHome ();

                // Emit the Ready event once we are first allowed
                // to load the home page (ensures we've downloaded
                // the fixup javascript, etc.).
                if (!IsReady) {
                    IsReady = true;
                    var handler = Ready;
                    handler?.Invoke (this, EventArgs.Empty);
                }
                return false;
            });
        }
    }
}
