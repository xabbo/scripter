using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Xabbo.Scripter.Services;

public class HabboUriProvider : IUriProvider<HabboEndpoints>
{
    private readonly Dictionary<HabboEndpoints, Uri> _endpoints = new();

    public string Domain { get; }

    public Uri this[HabboEndpoints endpoint] => _endpoints[endpoint];

    public HabboUriProvider(IConfiguration config)
    {
        Domain = config.GetValue<string>("Web:Domain");

        IConfigurationSection endpoints = config.GetSection("Web:Urls");
        foreach (IConfigurationSection endpointSection in endpoints.GetChildren())
        {
            string host = endpointSection.GetValue<string>("Host");
            Uri baseUri = new(host.Replace("$domain", Domain));

            foreach (IConfigurationSection pathSection in endpointSection.GetSection("Endpoints").GetChildren())
            {
                string endpointName = pathSection.Key;

                if (!Enum.TryParse(endpointName, out HabboEndpoints endpoint))
                {
                    throw new Exception($"Unknown Habbo endpoint name: '{endpointName}'.");
                }

                string relativePath = pathSection.Value;
                _endpoints[endpoint] = new Uri(baseUri, relativePath);
            }
        }
    }

    public Uri GetUri(HabboEndpoints endpoint) => _endpoints[endpoint];

    public Uri GetUri(HabboEndpoints endpoint, Dictionary<string, string> parameters)
    {
        throw new NotImplementedException();
    }
}
