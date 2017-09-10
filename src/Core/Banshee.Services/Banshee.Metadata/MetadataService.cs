//
// MetadataService.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc.
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Hyena;

using Banshee.Kernel;
using Banshee.Collection;

namespace Banshee.Metadata
{
    public class MetadataService : BaseMetadataProvider
    {
        private static MetadataService instance;

        public static MetadataService Instance {
            get {
                if (instance == null) {
                    instance = new MetadataService ();
                }

                return instance;
            }
        }

        private Dictionary<IBasicTrackInfo, IMetadataLookupJob> queries
            = new Dictionary<IBasicTrackInfo, IMetadataLookupJob> ();
        private List<IMetadataProvider> providers = new List<IMetadataProvider> ();

        public MetadataService ()
        {
            AddProvider (new Banshee.Metadata.Embedded.EmbeddedMetadataProvider ());
            AddProvider (new Banshee.Metadata.FileSystem.FileSystemMetadataProvider ());
            AddProvider (new Banshee.Metadata.MusicBrainz.MusicBrainzMetadataProvider ());
            AddProvider (new Banshee.Metadata.LastFM.LastFMMetadataProvider ());

            Scheduler.JobFinished += OnSchedulerJobFinished;
            Scheduler.JobUnscheduled += OnSchedulerJobUnscheduled;
        }

        public override IMetadataLookupJob CreateJob (IBasicTrackInfo track)
        {
            return new MetadataServiceJob (this, track);
        }

        public override void Lookup (IBasicTrackInfo track)
        {
            Lookup (track, JobPriority.Highest);
        }

        public void Lookup (IBasicTrackInfo track, JobPriority priority)
        {
            if (track == null || queries == null || track.ArtworkId == null) {
                return;
            }

            lock (((ICollection)queries).SyncRoot) {
                if (!queries.ContainsKey (track)) {
                    IMetadataLookupJob job = CreateJob (track);
                    if (job == null) {
                        return;
                    }

                    queries.Add (track, job);
                    Scheduler.Schedule (job, priority);
                }
            }
        }

        public void AddProvider (IMetadataProvider provider)
        {
            AddProvider (-1, provider);
        }

        public void AddProvider (int position, IMetadataProvider provider)
        {
            lock (providers) {
                if (position < 0) {
                    providers.Add (provider);
                } else {
                    providers.Insert (position, provider);
                }
            }
        }

        public void RemoveProvider (IMetadataProvider provider)
        {
            lock (providers) {
                providers.Remove (provider);
            }
        }

        private bool RemoveJob (IMetadataLookupJob job)
        {
            if (job == null || job.Track == null) {
                return false;
            }

            lock (((ICollection)queries).SyncRoot) {
                if (queries.ContainsKey (job.Track)) {
                    queries.Remove (job.Track);
                    return true;
                }

                return false;
            }
        }

        internal void OnHaveResult (MetadataServiceJob job)
        {
            ThreadAssist.ProxyToMain (delegate {
                OnHaveResult (job.Track, job.ResultTags);
            });
        }

        private void OnSchedulerJobFinished (IJob job)
        {
            if (!(job is IMetadataLookupJob)) {
                return;
            }

            IMetadataLookupJob lookup_job = (IMetadataLookupJob)job;
            RemoveJob (lookup_job);
        }

        private void OnSchedulerJobUnscheduled (IJob job)
        {
            RemoveJob (job as IMetadataLookupJob);
        }

        public ReadOnlyCollection<IMetadataProvider> Providers {
            get {
                lock (providers) {
                    return new ReadOnlyCollection<IMetadataProvider> (providers);
                }
            }
        }
    }
}
