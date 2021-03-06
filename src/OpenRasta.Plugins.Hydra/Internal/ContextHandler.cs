﻿using System.Collections.Generic;
using System.Linq;
using OpenRasta.Collections.Specialized;
using OpenRasta.Configuration.MetaModel;
using OpenRasta.Configuration.MetaModel.Handlers;
using OpenRasta.Plugins.Hydra.Configuration;
using OpenRasta.Plugins.Hydra.Schemas.Hydra;

namespace OpenRasta.Plugins.Hydra.Internal
{
  public class ContextHandler
  {
    readonly HydraOptions _options;
    readonly IEnumerable<(ResourceModel resource, HydraResourceModel hydra)> _defaultClasses;

    public ContextHandler(IMetaModelRepository metaModel)
    {
      _options = metaModel.CustomRegistrations.OfType<HydraOptions>().Single();
      _defaultClasses = (
        from resource in metaModel.ResourceRegistrations
        where resource.ResourceType != null
              && resource.Uris.Any(u=>u.Hydra().CollectionItemType == null)
        let hydraModel = resource.Hydra()
        where hydraModel.Vocabulary?.Uri == _options.Vocabulary.Uri
        select (resource, hydraModel)).ToList();
    }

    public Context Get()
    {
      return new Context
      {
        DefaultVocabulary = _options.Vocabulary.Uri.ToString(),
        Curies = _options.Curies.ToDictionary(v => v.DefaultPrefix, v => v.Uri),
        Classes = _defaultClasses.ToDictionary(
          c => c.resource.ResourceType.Name,
          c => $"{_options.Vocabulary.Uri}{c.resource.ResourceType.Name}/")
      };
    }
  }
}