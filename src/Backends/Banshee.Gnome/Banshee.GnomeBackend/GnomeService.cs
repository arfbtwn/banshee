//
// GnomeService.cs
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

using System;

using Banshee.ServiceStack;
using Banshee.Web;

namespace Banshee.GnomeBackend
{
    public class GnomeService : IExtensionService, IDisposable
    {
        private ProxyConfiguration proxy;

        private Brasero brasero;
        internal Brasero Brasero {
            get { return brasero; }
        }

        public GnomeService ()
        {
        }

        public void Initialize ()
        {
            try {
                // FIXME: this needs to be deferred/delayed initialized
                proxy = new ProxyConfiguration ();
            } catch (Exception e) {
                Hyena.Log.Error ("ProxyConfiguration", e);
                proxy = null;
            }

            try {
                brasero = new Brasero ();
                brasero.Initialize ();
            } catch {
                brasero = null;
            }

            if (Browser.OpenHandler == null) {
                Browser.OpenHandler = OpenUrl;
            }
        }

        public void Dispose ()
        {
            if (brasero != null) {
                brasero.Dispose ();
                brasero = null;
            }

            if (proxy != null) {
                proxy.Dispose ();
                proxy = null;
            }

            if (Browser.OpenHandler == (Banshee.Web.Browser.OpenUrlHandler) OpenUrl) {
                Browser.OpenHandler = null;
            }
        }

        private bool OpenUrl (string url)
        {
            Hyena.Log.Debug ("Opening URL via Gtk.Global.ShowUri", url);
            return Gtk.Global.ShowUri (url);
        }

        string IService.ServiceName {
            get { return "GnomeService"; }
        }
    }
}
