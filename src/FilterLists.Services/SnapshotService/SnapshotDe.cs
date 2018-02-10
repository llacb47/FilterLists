﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FilterLists.Data;
using FilterLists.Data.Entities;
using FilterLists.Data.Entities.Junctions;
using FilterLists.Services.Extensions;

namespace FilterLists.Services.SnapshotService
{
    public class SnapshotDe
    {
        private const int BatchSize = 1000;
        private readonly FilterListsDbContext dbContext;
        private readonly FilterListViewUrlDto list;
        private Snapshot snapshot;

        public SnapshotDe(FilterListsDbContext dbContext, FilterListViewUrlDto list)
        {
            this.dbContext = dbContext;
            this.list = list;
        }

        public async Task SaveSnapshotAsync()
        {
            var content = await CaptureSnapshot();
            if (content != null)
            {
                await SaveSnapshotInBatches(content);
                await DedupSnapshotRules();
            }
        }

        private async Task<string> CaptureSnapshot()
        {
            await AddSnapshot();
            var content = await TryGetContent();
            await dbContext.SaveChangesAsync();
            return content;
        }

        private async Task AddSnapshot()
        {
            snapshot = new Snapshot {FilterListId = list.Id};
            await dbContext.Snapshots.AddAsync(snapshot);
        }

        private async Task<string> TryGetContent()
        {
            try
            {
                return await GetContent();
            }
            catch (WebException we)
            {
                snapshot.HttpStatusCode = ((HttpWebResponse)we.Response).StatusCode.ToString();
                return null;
            }
            catch (HttpRequestException)
            {
                snapshot.HttpStatusCode = null;
                return null;
            }
        }

        private async Task<string> GetContent()
        {
            using (var httpClient = new HttpClient())
            using (var httpResponseMessage = await httpClient.GetAsync(list.ViewUrl))
            {
                snapshot.HttpStatusCode = httpResponseMessage.StatusCode.ToString();
                if (httpResponseMessage.IsSuccessStatusCode)
                    return await httpResponseMessage.Content.ReadAsStringAsync();
            }

            return null;
        }

        private async Task SaveSnapshotInBatches(string content)
        {
            var rawRules = GetRawRules(content);
            var snapshotBatches = GetSnapshotBatches(rawRules);
            await SaveSnapshotBatches(snapshotBatches);
        }

        private static IEnumerable<string> GetRawRules(string content)
        {
            var rawRules = content.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < rawRules.Length; i++)
                rawRules[i] = rawRules[i].LintRawRule();
            return new HashSet<string>(rawRules.Where(x => x != null));
        }

        private IEnumerable<SnapshotBatchDe> GetSnapshotBatches(IEnumerable<string> rawRules)
        {
            return rawRules.Batch(BatchSize)
                           .Select(rawRuleBatch => new SnapshotBatchDe(dbContext, snapshot, rawRuleBatch));
        }

        private static async Task SaveSnapshotBatches(IEnumerable<SnapshotBatchDe> snapshotBatches)
        {
            foreach (var snapshotBatch in snapshotBatches)
                await snapshotBatch.SaveSnapshotBatchAsync();
        }

        private async Task DedupSnapshotRules()
        {
            var existingSnapshotRules = GetExistingSnapshotRules();
            UpdateRemovedSnapshotRules(existingSnapshotRules);
            RemoveDuplicateSnapshotRules(existingSnapshotRules);
            await dbContext.SaveChangesAsync();
        }

        private IQueryable<SnapshotRule> GetExistingSnapshotRules()
        {
            return dbContext.SnapshotRules.Where(x =>
                x.AddedBySnapshot.FilterListId == list.Id &&
                x.AddedBySnapshot != snapshot &&
                x.RemovedBySnapshot == null);
        }

        private void UpdateRemovedSnapshotRules(IQueryable<SnapshotRule> existingSnapshotRules)
        {
            var newSnapshotRules = dbContext.SnapshotRules.Where(x => x.AddedBySnapshot == snapshot);
            var removedSnapshotRules = existingSnapshotRules.Where(x => !newSnapshotRules.Any(y => y.Rule == x.Rule));
            removedSnapshotRules.ToList().ForEach(x => x.RemovedBySnapshot = snapshot);
        }

        private void RemoveDuplicateSnapshotRules(IQueryable<SnapshotRule> existingSnapshotRules)
        {
            var duplicateSnapshotRules = dbContext.SnapshotRules.Where(x =>
                x.AddedBySnapshot == snapshot &&
                existingSnapshotRules.Any(y => y.Rule == x.Rule));
            dbContext.SnapshotRules.RemoveRange(duplicateSnapshotRules);
        }
    }
}