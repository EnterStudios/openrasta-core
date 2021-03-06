﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using OpenRasta.TypeSystem;
using OpenRasta.Web;

namespace OpenRasta.Codecs.Newtonsoft.Json
{
  [MediaType("application/json")]
  [MediaType("*/*;q=0.5")]
  public class NewtonsoftJsonCodec : IMediaTypeReaderAsync, IMediaTypeWriterAsync
  {
    readonly ICommunicationContext _context;
    JsonSerializerSettings _settings;

    object ICodec.Configuration
    {
      get => _settings;
      set => _settings = value as JsonSerializerSettings;
    }

    public NewtonsoftJsonCodec(ICommunicationContext context)
    {
      _context = context;
    }
    static JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Converters =
        {
            new StringEnumConverter()
        }
    };

    public async Task WriteTo(object entity, IHttpEntity response, IEnumerable<string> codecParameters)
    {
      var content = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(entity, _settings ?? DefaultSettings));
      response.ContentLength = content.Length;
      
      await response.Stream.WriteAsync(content, 0, content.Length);
    }

    public async Task<object> ReadFrom(IHttpEntity request, IType destinationType, string destinationName)
    {
      var content = await new StreamReader(request.Stream, Encoding.UTF8).ReadToEndAsync();
      return JsonConvert.DeserializeObject(content, _settings ?? DefaultSettings);
    }
  }
}