//
// StoreView.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//   Will Thompson <will@willthompson.co.uk>
//
// Copyright 2010 Novell, Inc.
// Copyright 2011 Will Thompson
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

using Gtk;

using Hyena;
using Hyena.Downloader;

using Banshee.Base;
using Banshee.IO;
using Banshee.WebBrowser;
using Banshee.WebSource;
using Banshee.Emusic;

namespace Banshee.Emusic.Store
{
    public class StoreView : WebView
    {
        private static bool IsEmusicContentType (string contentType)
        {
            return contentType == "application/vnd.emusic-emusic_list";
        }

        public StoreView ()
        {
            CanSearch = true;
            IsSignedIn = false;
            OssiferSession.CookieChanged += (o, n) => CheckSignIn ();
            CheckSignIn ();
            FullReload ();
        }

        protected override OssiferNavigationResponse OnMimeTypePolicyDecisionRequested (string mimetype)
        {
            // We only explicitly accept (render) text/html types, and only
            // download what we can import or preview.
            if (IsEmusicContentType (mimetype) || mimetype == "audio/x-mpegurl") {
                return OssiferNavigationResponse.Download;
            }

            return base.OnMimeTypePolicyDecisionRequested (mimetype);
        }

        protected override string OnDownloadRequested (string mimetype, string uri, string suggestedFilename)
        {
            if (IsEmusicContentType (mimetype)) {
                // BZZT BZZT! Secret "insecure temporary file" code detected.
                var dest_uri_base = "file://" + Paths.Combine (Paths.TempDir, suggestedFilename);
                var dest_uri = new SafeUri (dest_uri_base);
                for (int i = 1; File.Exists (dest_uri);
                    dest_uri = new SafeUri (String.Format ("{0} ({1})", dest_uri_base, ++i)));
                return dest_uri.AbsoluteUri;
            } else if (mimetype == "audio/x-mpegurl") {
                Banshee.Streaming.RadioTrackInfo.OpenPlay (uri);
                // Banshee.ServiceStack.ServiceManager.PlaybackController.StopWhenFinished = true;
                return null;
            }

            return null;
        }

        protected override void OnDownloadStatusChanged (OssiferDownloadStatus status, string mimetype, string destinationUri)
        {
            // FIXME: handle the error case
            if (status != OssiferDownloadStatus.Finished) {
                return;
            }

            if (IsEmusicContentType (mimetype)) {
                Log.Debug ("OssiferWebView: downloaded purchase list", destinationUri);
                Banshee.ServiceStack.ServiceManager.Get<EmusicService> ()
                    .ImportEmx (new SafeUri (destinationUri).LocalPath);
            }
        }

        public override void GoHome ()
        {
            LoadUri ("http://integrated-services.banshee.fm/emusic/home/");
        }

        public override void GoSearch (string query)
        {
            LoadUri ("http://integrated-services.banshee.fm/emusic/search/" + System.Uri.EscapeDataString(query));
        }

        public event EventHandler SignInChanged;
        public bool IsSignedIn { get; private set; }

        public void SignOut ()
        {
            LoadUri ("http://integrated-services.banshee.fm/emusic/sign_out/");
        }

        private void CheckSignIn ()
        {
            //bool signed_in = OssiferSession.GetCookie ("EMUSIC_REMEMBER_ME_COOKIE", "www.emusic.com", "/") != null;
            bool signed_in = false;

            if (IsSignedIn != signed_in) {
                IsSignedIn = signed_in;
                OnSignInChanged ();
            }
        }

        protected virtual void OnSignInChanged ()
        {
            var handler = SignInChanged;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }
    }
}
