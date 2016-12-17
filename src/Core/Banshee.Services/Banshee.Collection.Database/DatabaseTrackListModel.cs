//
// DatabaseTrackListModel.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Text;
using System.Collections.Generic;
using System.Linq;

using Mono.Unix;

using Hyena;
using Hyena.Data;
using Hyena.Data.Sqlite;
using Hyena.Query;

using Banshee.Base;
using Banshee.Query;
using Banshee.Database;
using Banshee.PlaybackController;

namespace Banshee.Collection.Database
{
    public class DatabaseTrackListModel : TrackListModel, IExportableModel,
        ICacheableDatabaseModel, IFilterable, ISortable, ICareAboutView, ISearchable
    {
        private readonly BansheeDbConnection connection;
        private IDatabaseTrackModelProvider provider;
        protected IDatabaseTrackModelCache cache;
        private Banshee.Sources.DatabaseSource source;

        private long count;

        private long filtered_count;
        private TimeSpan filtered_duration, duration;
        private long filtered_filesize, filesize;

        private ISortableColumn sort_column;
        private string sort_query;
        private bool forced_sort_query;

        private string reload_fragment;
        private string join_table, join_fragment, join_primary_key, join_column, condition, condition_from;

        private string query_fragment;
        private string user_query;

        private int rows_in_view;

        public DatabaseTrackListModel (BansheeDbConnection connection, IDatabaseTrackModelProvider provider, Banshee.Sources.DatabaseSource source)
        {
            this.connection = connection;
            this.provider = provider;
            this.source = source;

            SelectAggregates = String.Format ("SUM({0}), SUM({1})",
                                              BansheeQuery.DurationField.Column, BansheeQuery.FileSizeField.Column);

            Selection.Changed += delegate {
                if (SelectionAggregatesHandler != null) {
                    cache.UpdateSelectionAggregates (SelectionAggregatesHandler);
                }
            };
        }

        protected Action<IDataReader> SelectionAggregatesHandler { get; set; }

        protected HyenaSqliteConnection Connection {
            get { return connection; }
        }

        private bool initialized = false;
        public void Initialize (IDatabaseTrackModelCache cache)
        {
            if (initialized)
                return;

            initialized = true;
            this.cache = cache;
            cache.AggregatesUpdated += HandleCacheAggregatesUpdated;
            GenerateSortQueryPart ();
        }

        private bool have_new_user_query = true;
        private void GenerateUserQueryFragment ()
        {
            if (!have_new_user_query)
                return;

            if (String.IsNullOrEmpty (UserQuery)) {
                query_fragment = null;
                query_tree = null;
            } else {
                query_tree = UserQueryParser.Parse (UserQuery, BansheeQuery.FieldSet);
                query_fragment = (query_tree == null) ? null : query_tree.ToSql (BansheeQuery.FieldSet);

                if (query_fragment != null && query_fragment.Length == 0) {
                    query_fragment = null;
                    query_tree = null;
                }
            }

            have_new_user_query = false;
        }

        private QueryNode query_tree;
        public QueryNode Query {
            get { return query_tree; }
        }

        protected string SortQuery {
            get { return sort_query; }
            set { sort_query = value; }
        }

        protected virtual void GenerateSortQueryPart ()
        {
            SortQuery = (SortColumn == null || SortColumn.SortType == SortType.None)
                ? (SortColumn != null && source is Banshee.Playlist.PlaylistSource)
                    ? "CorePlaylistEntries.ViewOrder ASC, CorePlaylistEntries.EntryID ASC"
                    : BansheeQuery.GetSort (BansheeQuery.ArtistField, true)
                : BansheeQuery.GetSort (SortColumn.Field, SortColumn.SortType == SortType.Ascending);
        }

        private SortType last_sort_type = SortType.None;
        public bool Sort (ISortableColumn column)
        {
            lock (this) {
                if (forced_sort_query) {
                    return false;
                }

                // Don't sort by the same column and the same sort-type more than once
                if (sort_column != null && sort_column == column && column.SortType == last_sort_type) {
                    return false;
                }

                last_sort_type = column.SortType;
                sort_column = column;

                GenerateSortQueryPart ();
                cache.Clear ();
            }
            return true;
        }

        public void Resort ()
        {
            var column = sort_column;
            sort_column = null;
            Sort (column);
        }

        private void HandleCacheAggregatesUpdated (IDataReader reader)
        {
            filtered_duration = TimeSpan.FromMilliseconds (reader[1] == null ? 0 : Convert.ToInt64 (reader[1]));
            filtered_filesize = reader[2] == null ? 0 : Convert.ToInt64 (reader[2]);
        }

        public override void Clear ()
        {
            cache.Clear ();
            count = 0;
            filesize = 0;
            duration = TimeSpan.Zero;
            filtered_count = 0;
            OnCleared ();
        }

        public void InvalidateCache (bool notify)
        {
            if (cache == null) {
                Log.ErrorFormat ("Called invalidate cache for {0}'s track model, but cache is null", source);
            } else {
                cache.Clear ();
                if (notify) {
                    OnReloaded ();
                }
            }
        }

        private string unfiltered_query;
        public virtual string UnfilteredQuery {
            get {
                return unfiltered_query ?? (unfiltered_query = String.Format (
                    "FROM {0} WHERE {1} {2}",
                    FromFragment,
                    String.IsNullOrEmpty (provider.Where) ? "1=1" : provider.Where,
                    ConditionFragment
                ));
            }
        }

        private string from;
        protected string From {
            get {
                if (from == null) {
                    from = provider.From;
                    int i = from.IndexOf (',');
                    if (i > 0) {
                        // Force the join order to fix bgo#581103 and bgo#603661
                        // See section 5.2 in http://www.sqlite.org/optoverview.html
                        from = from.Substring (0, i) + " CROSS JOIN " + from.Substring (i + 1);
                    }
                }
                return from;
            }
            set { from = value; }
        }

        private string from_fragment;
        public string FromFragment {
            get { return from_fragment ?? (from_fragment = String.Format ("{0}{1}", From, JoinFragment)); }
        }

        public virtual void UpdateUnfilteredAggregates ()
        {
            HyenaSqliteCommand count_command = new HyenaSqliteCommand (String.Format (
                "SELECT COUNT(*), SUM({0}), SUM({1}) {2}",
                BansheeQuery.FileSizeField.Column, BansheeQuery.DurationField.Column, UnfilteredQuery
            ));

            using (HyenaDataReader reader = new HyenaDataReader (connection.Query (count_command))) {
                count = reader.Get<long> (0);
                filesize = reader.Get<long> (1);
                duration = reader.Get<TimeSpan> (2);
            }
        }

        public override void Reload ()
        {
            Reload (null);
        }

        public void Reload (IListModel reloadTrigger)
        {
            if (cache == null) {
                Log.WarningFormat ("Called Reload on {0} for source {1} but cache is null;  Did you forget to call AfterInitialized () in your DatabaseSource ctor?",
                    this, source == null ? "null source!" : source.Name);
                return;
            }

            lock (this) {
                GenerateUserQueryFragment ();

                UpdateUnfilteredAggregates ();
                cache.SaveSelection ();

                Log.DebugFormat ("Reload trigger: {0}", reloadTrigger);

                var reload_models = new List<IFilterListModel> ();

                if (null == reloadTrigger) {
                    reload_models.AddRange (source.CurrentFilters);
                } else {
                    reload_models.AddRange (source.CurrentFilters.SkipWhile (x => x != reloadTrigger).Skip(1));
                }

                if (reload_models.Count == 0) {
                    ReloadWithFilters (true);
                } else {
                    ReloadWithoutFilters ();

                    foreach (IFilterListModel model in reload_models) {
                        model.Reload (false);
                    }

                    bool have_filters = false;
                    foreach (IFilterListModel filter in source.CurrentFilters) {
                        have_filters |= !filter.Selection.AllSelected;
                    }

                    // Unless both artist/album selections are "all" (eg unfiltered), reload
                    // the track model again with the artist/album filters now in place.
                    if (have_filters) {
                        ReloadWithFilters (true);
                    }
                }

                cache.UpdateAggregates ();
                cache.RestoreSelection ();

                if (SelectionAggregatesHandler != null) {
                    cache.UpdateSelectionAggregates (SelectionAggregatesHandler);
                }

                filtered_count = cache.Count;

                OnReloaded ();

                // Trigger these after the track list, b/c visually it's more important for it to update first
                foreach (IFilterListModel model in reload_models) {
                    model.RaiseReloaded ();
                }
            }
        }

        private void ReloadWithoutFilters ()
        {
            ReloadWithFilters (false);
        }

        private void ReloadWithFilters (bool with_filters)
        {
            StringBuilder qb = new StringBuilder ();
            qb.Append (UnfilteredQuery);

            if (with_filters) {
                foreach (IFilterListModel filter in source.CurrentFilters) {
                    string filter_sql = filter.GetSqlFilter ();
                    if (filter_sql != null) {
                        qb.Append (" AND ");
                        qb.Append (filter_sql);
                    }
                }
            }

            if (query_fragment != null) {
                qb.Append (" AND ");
                qb.Append (query_fragment);
            }

            if (sort_query != null) {
                qb.Append (" ORDER BY ");
                qb.Append (sort_query);
            }

            reload_fragment = qb.ToString ();
            cache.Reload ();
        }

        private QueryFieldSet query_fields = BansheeQuery.FieldSet;
        public QueryFieldSet QueryFields {
            get { return query_fields; }
            protected set { query_fields = value; }
        }

        public bool Contains (DatabaseTrackInfo track)
        {
            return track != null && connection.Query<bool> (
                String.Format ("SELECT COUNT(*) > 0 {0} AND CoreTracks.TrackID = ? LIMIT 1", UnfilteredQuery),
                track.TrackId
            );
        }

        public int IndexOf (QueryNode query, long offset)
        {
            lock (this) {
                if (query == null) {
                    return -1;
                }

                return (int) cache.IndexOf (query.ToSql (QueryFields), offset);
            }
        }

        public override int IndexOf (TrackInfo track)
        {
            lock (this) {
                if (track is DatabaseTrackInfo) {
                    return (int) cache.IndexOf (track as DatabaseTrackInfo);
                } else if (track is Banshee.Streaming.RadioTrackInfo) {
                    return (int) cache.IndexOf ((track as Banshee.Streaming.RadioTrackInfo).ParentTrack as DatabaseTrackInfo);
                }
                return -1;
            }
        }

        public int IndexOfFirst (TrackInfo track)
        {
            lock (this) {
                return IndexOf (cache.GetSingleWhere ("AND MetadataHash = ? ORDER BY OrderID", track.MetadataHash));
            }
        }

        public override TrackInfo GetRandom (DateTime notPlayedSince)
        {
            return GetRandom (notPlayedSince, "song", true, false, Shuffler.Playback);
        }

        public TrackInfo GetRandom (DateTime notPlayedSince, string shuffle_mode, bool repeat, bool resetSinceTime, Shuffler shuffler)
        {
            lock (this) {
                shuffler.SetModelAndCache (this, cache);
                return shuffler.GetRandom (notPlayedSince, shuffle_mode, repeat, resetSinceTime);
            }
        }

        public override TrackInfo this[int index] {
            get {
                lock (this) {
                    return cache.GetValue (index);
                }
            }
        }

        public override int Count {
            get { return (int) filtered_count; }
        }

        public virtual TimeSpan Duration {
            get { return filtered_duration; }
        }

        public virtual long FileSize {
            get { return filtered_filesize; }
        }

        public long UnfilteredFileSize {
            get { return filesize; }
        }

        public TimeSpan UnfilteredDuration {
            get { return duration; }
        }

        public int UnfilteredCount {
            get { return (int) count; }
            set { count = value; }
        }

        public string UserQuery {
            get { return user_query; }
            set {
                lock (this) {
                    user_query = value;
                    have_new_user_query = true;
                }
            }
        }

        public string ForcedSortQuery {
            get { return forced_sort_query ? sort_query : null; }
            set {
                forced_sort_query = value != null;
                sort_query = value;
                if (cache != null) {
                    cache.Clear ();
                }
            }
        }

        public string JoinTable {
            get { return join_table; }
            set {
                join_table = value;
                join_fragment = String.Format (", {0}", join_table);
            }
        }

        public string JoinFragment {
            get { return join_fragment; }
        }

        public string JoinPrimaryKey {
            get { return join_primary_key; }
            set { join_primary_key = value; }
        }

        public string JoinColumn {
            get { return join_column; }
            set { join_column = value; }
        }

        public void AddCondition (string part)
        {
            AddCondition (null, part);
        }

        public void AddCondition (string tables, string part)
        {
            if (!String.IsNullOrEmpty (part)) {
                condition = condition == null ? part : String.Format ("{0} AND {1}", condition, part);

                if (!String.IsNullOrEmpty (tables)) {
                    condition_from = condition_from == null ? tables : String.Format ("{0}, {1}", condition_from, tables);
                }
            }
        }

        public string Condition {
            get { return condition; }
        }

        private string condition_from_fragment;
        public string ConditionFromFragment {
            get {
                if (condition_from_fragment == null) {
                    if (JoinFragment == null) {
                        condition_from_fragment = condition_from;
                    } else {
                        if (condition_from == null) {
                            condition_from = "CoreTracks";
                        }

                        condition_from_fragment = String.Format ("{0}{1}", condition_from, JoinFragment);
                    }
                }

                return condition_from_fragment;
            }
        }

        public string ConditionFragment {
            get { return PrefixCondition ("AND"); }
        }

        private string PrefixCondition (string prefix)
        {
            string condition = Condition;
            return String.IsNullOrEmpty (condition)
                ? String.Empty
                : String.Format (" {0} {1} ", prefix, condition);
        }

        public int CacheId {
            get { return (int) cache.CacheId; }
        }

        public ISortableColumn SortColumn {
            get { return sort_column; }
        }

        public virtual int RowsInView {
            protected get { return rows_in_view; }
            set { rows_in_view = value; }
        }

        int IExportableModel.GetLength ()
        {
            return Count;
        }

        IDictionary<string, object> IExportableModel.GetMetadata (int index)
        {
            return this[index].GenerateExportable ();
        }

        private string track_ids_sql;
        public string TrackIdsSql {
            get {
                if (track_ids_sql == null) {
                    if (!CachesJoinTableEntries) {
                        track_ids_sql = "ItemID FROM CoreCache WHERE ModelID = ? LIMIT ?, ?";
                    } else {
                        track_ids_sql = String.Format (
                            "{0} FROM {1} WHERE {2} IN (SELECT ItemID FROM CoreCache WHERE ModelID = ? LIMIT ?, ?)",
                            JoinColumn, JoinTable, JoinPrimaryKey
                        );
                    }
                }
                return track_ids_sql;
            }
        }

        private bool caches_join_table_entries = false;
        public bool CachesJoinTableEntries {
            get { return caches_join_table_entries; }
            set { caches_join_table_entries = value; }
        }

        // Implement ICacheableModel
        public int FetchCount {
            get { return RowsInView > 0 ? RowsInView * 5 : 100; }
        }

        public string SelectAggregates { get; protected set; }

        // Implement IDatabaseModel
        public string ReloadFragment {
            get { return reload_fragment; }
        }

        public bool CachesValues { get { return false; } }
    }
}
