﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using OpenRasta.Configuration;
using OpenRasta.Configuration.Fluent;
using OpenRasta.Configuration.Fluent.Extensions;
using OpenRasta.Configuration.MetaModel;
using OpenRasta.Configuration.MetaModel.Handlers;
using OpenRasta.Plugins.ReverseProxy.HttpClientFactory;
using OpenRasta.Plugins.ReverseProxy.HttpMessageHandlers;

namespace OpenRasta.Plugins.ReverseProxy
{
  public static class FluentApiExtensions
  {
    public static void ReverseProxyFor(this IUriDefinition uriConfiguration, string uri)
    {
      uriConfiguration
        .HandledBy<ReverseProxyHandler>();

      var target = (IResourceTarget) uriConfiguration;
      target.Resource.ReverseProxyTarget(uri);
    }

    public static T ReverseProxy<T>(this T uses, ReverseProxyOptions options = null) where T : IUses
    {
      options = options ?? new ReverseProxyOptions();

      if (options.HttpClient.RoundRobin.Enabled)
      {
        var handler = options.HttpClient.Handler;
        if (options.HttpClient.RoundRobin.ClientPerNode)
          handler = () => new LockToIPAddress(options.HttpClient.Handler(), options.HttpClient.RoundRobin.DnsResolver);

        var factory = new RoundRobinHttpClientFactory(
          options.HttpClient.RoundRobin.ClientCount,
          handler,
          options.HttpClient.RoundRobin.LeaseTime);

        uses.Dependency(d => d.Singleton(() => new ReverseProxy(
          options.Timeout,
          options.FrowardedHeaders.ConvertLegacyHeaders,
          options.Via.Pseudonym,
          factory.GetClient
        )));
      }
      else
      {
        uses.Dependency(d => d.Singleton(() => new ReverseProxy(
          options.Timeout,
          options.FrowardedHeaders.ConvertLegacyHeaders,
          options.Via.Pseudonym,
          options.HttpClient.Factory)));
      }

      var has = (IHas) uses;
      has.ResourcesOfType<ReverseProxyResponse>()
        .WithoutUri
        .TranscodedBy<ReverseProxyResponseCodec>()
        .ForMediaType("*/*");

      if (options.FrowardedHeaders.RunAsForwardedHost)
        uses.PipelineContributor<RewriteAppBaseUsingForwardedHeaders>();

      return uses;
    }
  }
}