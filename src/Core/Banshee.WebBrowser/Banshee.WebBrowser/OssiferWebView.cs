// 
// OssiferWebView.cs
// 
// Author:
//   Aaron Bockover <abockover@novell.com>
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
using Gtk;
using EventArgs = System.EventArgs;
using EventHandler = System.EventHandler;

namespace Banshee.WebBrowser
{
    public class OssiferWebView : Widget
    {
        private const string Libossifer = "ossifer";

        protected OssiferWebView (IntPtr raw) : base (raw)
        {
        }

        public OssiferWebView ()
        {
            OssiferSession.Initialize ();

            var callbacks = new Callbacks
            {
                MimeTypePolicyDecisionRequested =
                    HandleMimeTypePolicyDecisionRequested,
                NavigationPolicyDecisionRequested =
                    HandleNavigationPolicyDecisionRequested,
                DownloadRequested = HandleDownloadRequested,
                ResourceRequestStarting = HandleResourceRequestStarting,
                LoadStatusChanged = HandleLoadStatusChanged,
                DownloadStatusChanged = HandleDownloadStatusChanged
            };

            ossifer_web_view_set_callbacks (Handle, callbacks);
        }

        public new static GType GType => new GType (ossifer_web_view_get_type ());

        public event EventHandler LoadStatusChanged;
        public event Action<float> ZoomChanged;

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_web_view_get_type ();

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_set_callbacks (IntPtr ossifer, Callbacks callbacks);

        private delegate OssiferNavigationResponse MimeTypePolicyDecisionRequestedCallback (IntPtr ossifer,
            IntPtr mimetype);

        private delegate OssiferNavigationResponse NavigationPolicyDecisionRequestedCallback (IntPtr ossifer,
            IntPtr uri);

        private delegate IntPtr DownloadRequestedCallback (IntPtr ossifer, IntPtr mimetype, IntPtr uri,
            IntPtr suggestedFilename);

        private delegate IntPtr ResourceRequestStartingCallback (IntPtr ossifer, IntPtr uri);

        private delegate void LoadStatusChangedCallback (IntPtr ossifer, OssiferLoadStatus status);

        private delegate void DownloadStatusChangedCallback (IntPtr ossifer, OssiferDownloadStatus status,
            IntPtr mimetype, IntPtr destnationUri);

        [StructLayout (LayoutKind.Sequential)]
        private struct Callbacks
        {
            public MimeTypePolicyDecisionRequestedCallback MimeTypePolicyDecisionRequested;
            public NavigationPolicyDecisionRequestedCallback NavigationPolicyDecisionRequested;
            public DownloadRequestedCallback DownloadRequested;
            public ResourceRequestStartingCallback ResourceRequestStarting;
            public LoadStatusChangedCallback LoadStatusChanged;
            public DownloadStatusChangedCallback DownloadStatusChanged;
        }

        #region Callback Implementations

        private OssiferNavigationResponse HandleMimeTypePolicyDecisionRequested (IntPtr ossifer, IntPtr mimetype)
        {
            return OnMimeTypePolicyDecisionRequested (Marshaller.Utf8PtrToString (mimetype));
        }

        protected virtual OssiferNavigationResponse OnMimeTypePolicyDecisionRequested (string mimetype)
        {
            return OssiferNavigationResponse.Unhandled;
        }

        private OssiferNavigationResponse HandleNavigationPolicyDecisionRequested (IntPtr ossifer, IntPtr uri)
        {
            return OnNavigationPolicyDecisionRequested (Marshaller.Utf8PtrToString (uri));
        }

        protected virtual OssiferNavigationResponse OnNavigationPolicyDecisionRequested (string uri)
        {
            return OssiferNavigationResponse.Unhandled;
        }

        private IntPtr HandleDownloadRequested (IntPtr ossifer, IntPtr mimetype, IntPtr uri, IntPtr suggestedFilename)
        {
            var destination_uri = OnDownloadRequested (
                Marshaller.Utf8PtrToString (mimetype),
                Marshaller.Utf8PtrToString (uri),
                Marshaller.Utf8PtrToString (suggestedFilename));
            return destination_uri == null
                ? IntPtr.Zero
                : Marshaller.StringToPtrGStrdup (destination_uri);
        }

        protected virtual string OnDownloadRequested (string mimetype, string uri, string suggestedFilename)
        {
            return null;
        }

        private IntPtr HandleResourceRequestStarting (IntPtr ossifer, IntPtr oldUri)
        {
            var new_uri = OnResourceRequestStarting (Marshaller.Utf8PtrToString (oldUri));
            return new_uri == null
                ? IntPtr.Zero
                : Marshaller.StringToPtrGStrdup (new_uri);
        }

        protected virtual string OnResourceRequestStarting (string oldUri)
        {
            return null;
        }

        private void HandleLoadStatusChanged (IntPtr ossifer, OssiferLoadStatus status)
        {
            OnLoadStatusChanged (status);
        }

        protected virtual void OnLoadStatusChanged (OssiferLoadStatus status)
        {
            var handler = LoadStatusChanged;
            handler?.Invoke (this, EventArgs.Empty);
        }

        private void HandleDownloadStatusChanged (IntPtr ossifer, OssiferDownloadStatus status, IntPtr mimetype,
            IntPtr destinationUri)
        {
            OnDownloadStatusChanged (status,
                Marshaller.Utf8PtrToString (mimetype),
                Marshaller.Utf8PtrToString (destinationUri));
        }

        protected virtual void OnDownloadStatusChanged (OssiferDownloadStatus status, string mimetype,
            string destinationUri)
        {
        }

        #endregion

        #region Public Instance API

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_web_view_load_uri (IntPtr ossifer, IntPtr uri);

        public void LoadUri (string uri)
        {
            var uri_raw = IntPtr.Zero;
            try {
                uri_raw = Marshaller.StringToPtrGStrdup (uri);
                ossifer_web_view_load_uri (Handle, uri_raw);
            }
            finally {
                Marshaller.Free (uri_raw);
            }
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_load_string (IntPtr ossifer,
            IntPtr content, IntPtr mimetype, IntPtr encoding, IntPtr baseUri);

        public void LoadString (string content, string mimetype, string encoding, string baseUri)
        {
            var content_raw = IntPtr.Zero;
            var mimetype_raw = IntPtr.Zero;
            var encoding_raw = IntPtr.Zero;
            var base_uri_raw = IntPtr.Zero;

            try {
                ossifer_web_view_load_string (Handle,
                    content_raw = Marshaller.StringToPtrGStrdup (content),
                    mimetype_raw = Marshaller.StringToPtrGStrdup (mimetype),
                    encoding_raw = Marshaller.StringToPtrGStrdup (encoding),
                    base_uri_raw = Marshaller.StringToPtrGStrdup (baseUri));
            }
            finally {
                Marshaller.Free (content_raw);
                Marshaller.Free (mimetype_raw);
                Marshaller.Free (encoding_raw);
                Marshaller.Free (base_uri_raw);
            }
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ossifer_web_view_can_go_forward (IntPtr ossifer);

        public virtual bool CanGoForward => ossifer_web_view_can_go_forward (Handle);

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern bool ossifer_web_view_can_go_back (IntPtr ossifer);

        public virtual bool CanGoBack => ossifer_web_view_can_go_back (Handle);

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_go_forward (IntPtr ossifer);

        public virtual void GoForward ()
        {
            ossifer_web_view_go_forward (Handle);
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_go_back (IntPtr ossifer);

        public virtual void GoBack ()
        {
            ossifer_web_view_go_back (Handle);
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_set_zoom (IntPtr ossifer, float zoomLevel);

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern float ossifer_web_view_get_zoom (IntPtr ossifer);

        public float Zoom {
            get => ossifer_web_view_get_zoom (Handle);
            set {
                ossifer_web_view_set_zoom (Handle, value);
                var handler = ZoomChanged;
                handler?.Invoke (value);
            }
        }

        private const float ZoomStep = 0.05f;

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
            //TODO: Handle smooth scroll events when gtk-sharp exposes deltaX/Y fields.
            //      See https://github.com/arfbtwn/banshee/issues/37
            /*if ((scroll.State & Gdk.ModifierType.ControlMask) != 0) {
                Zoom += (scroll.Direction == Gdk.ScrollDirection.Up) ? ZOOM_STEP : -ZOOM_STEP;
                return true;
            }*/

            return base.OnScrollEvent (scroll);
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_reload (IntPtr ossifer);

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_reload_bypass_cache (IntPtr ossifer);

        public virtual void Reload (bool bypassCache)
        {
            if (bypassCache)
                ossifer_web_view_reload_bypass_cache (Handle);
            else
                ossifer_web_view_reload (Handle);
        }

        public void Reload ()
        {
            Reload (false);
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern void ossifer_web_view_execute_script (IntPtr ossifer, IntPtr script);

        public void ExecuteScript (string script)
        {
            var script_raw = IntPtr.Zero;
            try {
                ossifer_web_view_execute_script (Handle, script_raw = Marshaller.StringToPtrGStrdup (script));
            }
            finally {
                Marshaller.Free (script_raw);
            }
        }

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_web_view_get_uri (IntPtr ossifer);

        public virtual string Uri => Marshaller.Utf8PtrToString (ossifer_web_view_get_uri (Handle));

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ossifer_web_view_get_title (IntPtr ossifer);

        public virtual string Title => Marshaller.Utf8PtrToString (ossifer_web_view_get_title (Handle));

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern OssiferLoadStatus ossifer_web_view_get_load_status (IntPtr ossifer);

        public virtual OssiferLoadStatus LoadStatus => ossifer_web_view_get_load_status (Handle);

        [DllImport (Libossifer, CallingConvention = CallingConvention.Cdecl)]
        private static extern OssiferSecurityLevel ossifer_web_view_get_security_level (IntPtr ossifer);

        public virtual OssiferSecurityLevel SecurityLevel => ossifer_web_view_get_security_level (Handle);

        #endregion
    }
}
