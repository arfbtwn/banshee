//
// PotentialSource.cs
//
// Author:
//   Nicholas Little <arealityfarbetween@googlemail.com>
//
// Copyright (C) 2014 Nicholas Little
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

using Mono.Unix;

using Banshee.Collection.Database;
using Banshee.Dap.Gui;
using Banshee.Hardware;
using Banshee.Sources;
using Banshee.Sources.Gui;

using Hyena;

using Mono.Addins;

namespace Banshee.Dap
{
    public abstract class PotentialSource : DapSource
    {
        private object lock_object = new object();

        private bool initialized;

        protected PotentialSource () { }

        #region overridden members of Source

        protected override void Initialize ()
        {
            Initialize (false);
        }

        protected void Initialize (bool active)
        {
            base.Initialize ();

            ThreadAssist.BlockingProxyToMain (() => {
                if (!active) {
                    ClearChildSources ();
                    Properties.Set<ISourceContents> ("Nereid.SourceContents", new InactiveDapContent (this));
                }
                DapContent.UpdateActions ();
            });
        }

        #endregion

        #region implemented abstract members of RemovableSource

        public override bool CanImport {
            get { return false; }
        }

        public override bool IsReadOnly {
            get { return true; }
        }

        public override bool IsConnected {
            get { return false; }
        }

        #endregion

        #region implemented abstract members of DapSource

        private bool TryDeviceInitialize ()
        {
            lock (lock_object) {

                if (initialized) return true;

                SetStatus (
                    Catalog.GetString ("Trying to claim your device..."),
                    false,
                    true,
                    "dialog-information"
                );
                try {
                    initialized = Claim ();
                } catch (InvalidDeviceStateException e) {
                    Log.Warning (e);
                } catch (InvalidDeviceException e) {
                    Log.Warning (e);
                } catch (Exception e) {
                    Log.Error (e);
                }

                SetStatus (
                    initialized ? Catalog.GetString ("Connection successful. Please wait...")
                            : Catalog.GetString ("Connection failed"),
                    !initialized,
                    initialized,
                    initialized ? "dialog-information"
                            : "dialog-warning"
                );

                return initialized;
            }
        }
        #endregion

        protected abstract bool Claim ();

        internal void TryClaim ()
        {
            ThreadAssist.SpawnFromMain (() => {
                if (TryDeviceInitialize ()) {
                    LoadDeviceContents ();
                }
            });
        }
    }
}

