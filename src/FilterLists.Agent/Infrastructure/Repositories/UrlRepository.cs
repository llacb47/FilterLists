﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using FilterLists.Agent.Core.Interfaces;
using FilterLists.Agent.Features.Urls.Models.DataFileUrls;
using FilterLists.Agent.Infrastructure.Clients;
using Microsoft.Extensions.Localization;
using RestSharp;

namespace FilterLists.Agent.Infrastructure.Repositories
{
    public class UrlRepository : IUrlRepository
    {
        private static readonly Dictionary<string, string> EntityUrlsEndpoints = new Dictionary<string, string>
        {
            {nameof(LicenseUrls), "licenses"},
            {nameof(ListUrls), "lists"},
            {nameof(MaintainerUrls), "maintainers"},
            {nameof(SoftwareUrls), "software"},
            {nameof(SyntaxUrls), "syntaxes"}
        };

        private readonly IFilterListsApiClient _apiClient;
        private readonly IStringLocalizer<UrlRepository> _localizer;

        public UrlRepository(IFilterListsApiClient apiClient, IStringLocalizer<UrlRepository> stringLocalizer)
        {
            _apiClient = apiClient;
            _localizer = stringLocalizer;
        }

        public async Task<IEnumerable<Uri>> GetAllAsync<TModel>()
        {
            if (!EntityUrlsEndpoints.ContainsKey(typeof(TModel).Name))
                throw new InvalidEnumArgumentException(_localizer["The type of TModel is not valid."]);
            var request = new RestRequest($"{EntityUrlsEndpoints[typeof(TModel).Name]}/seed");
            var response = await _apiClient.ExecuteAsync<IEnumerable<TModel>>(request);
            return response.SelectMany(r =>
                r.GetType().GetProperties().Where(p => p.GetValue(r) != null).Select(p => (Uri)p.GetValue(r)));
        }
    }
}