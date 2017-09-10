//
// DapActions.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
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
using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Widgets;

using Banshee.Dap;
using Banshee.Gui;
using Banshee.ServiceStack;

namespace Banshee.Dap.Gui
{
    public class DapActions : BansheeActionGroup
    {
        private DapSource previous_dap = null;
        private DapSource Dap {
            get { return Actions.SourceActions.ActionSource as DapSource; }
        }

        public DapActions () : base ("dap")
        {
            AddImportant (
                new ActionEntry ("ClaimDapAction", null,
                    Catalog.GetString ("Claim"), null,
                    Catalog.GetString ("Claim"), OnClaimDap)
            );
            AddImportant (
                new ActionEntry ("SyncDapAction", null,
                    Catalog.GetString ("Sync"), null,
                    Catalog.GetString ("Sync"), OnSyncDap)
            );

            AddUiFromFile ("GlobalUI.xml");

            this["SyncDapAction"].IconName = Stock.Refresh;
            this["ClaimDapAction"].IconName = Stock.Connect;
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;
            Actions.SourceActions.Updated += delegate { UpdateActions (); };
            OnActiveSourceChanged (null);

            Register ();
        }

        private void OnActiveSourceChanged (EventArgs args)
        {
            if (previous_dap != null) {
                previous_dap.Sync.Updated -= OnSyncUpdated;
            }

            previous_dap = ActiveSource as DapSource;
            UpdateActions ();

            if (previous_dap != null) {
                previous_dap.Sync.Updated += OnSyncUpdated;
            }
        }

        private void OnSyncUpdated (DapSync sync)
        {
            ThreadAssist.ProxyToMain (UpdateActions);
        }

        internal void UpdateActions ()
        {
            DapSource dap = Dap;
            if (dap != null) {
                UpdateAction ("SyncDapAction", dap.IsConnected && dap.Sync.Enabled);
                UpdateAction ("ClaimDapAction", !dap.IsConnected);
            }
        }

        private void OnSyncDap (object o, EventArgs args)
        {
            DapSource dap = Dap;
            if (dap != null) {
                dap.Sync.Sync ();
            }
        }

        private void OnClaimDap (object o, EventArgs args)
        {
            var dap = Dap as PotentialSource;
            if (dap != null) {
                dap.TryClaim ();
            }
        }
    }
}
