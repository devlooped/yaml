using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpYaml.Serialization;

/// <summary>
/// Returns values as specified by a JSONPath Query from a YAML file.
/// </summary>
public class YamlPeek : Task
{
    /// <summary>
    /// Specifies the YAML input as a string.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Specifies the YAML input as a file path.
    /// </summary>
    public ITaskItem? ContentPath { get; set; }

    /// <summary>
    /// Specifies the JSONPath query.
    /// </summary>
    [Required]
    public string Query { get; set; } = "$";

    /// <summary>
    /// Contains the results that are returned by the task.
    /// </summary>
    [Output]
    public ITaskItem[] Result { get; private set; } = [];

    /// <summary>
    /// Executes the <see cref="Query"/> against either the 
    /// <see cref="Content"/> or <see cref="ContentPath"/> JSON.
    /// </summary>
    public override bool Execute()
    {
        if (Content == null && ContentPath == null)
            return Log.Error("JPE01", $"Either {nameof(Content)} or {nameof(ContentPath)} must be provided.");

        if (ContentPath != null && !File.Exists(ContentPath.GetMetadata("FullPath")))
            return Log.Error("JPE02", $"Specified {nameof(ContentPath)} not found at {ContentPath.GetMetadata("FullPath")}.");

        if (Content != null && ContentPath != null)
            return Log.Error("JPE03", $"Cannot specify both {nameof(Content)} and {nameof(ContentPath)}.");

        var content = ContentPath != null ?
            File.ReadAllText(ContentPath.GetMetadata("FullPath")) : Content;

        if (content is null || string.IsNullOrEmpty(content))
            return Log.Warn("JPE04", $"Empty JSON content.", true);

        var data = new Serializer(new SerializerSettings
        {
            ObjectFactory = ExpandoObjectFactory.Default,
            EmitTags = false,
        }).Deserialize(content);

        var json = JsonConvert.SerializeObject(data);
        var jobj = JToken.Parse(json);

        Result = jobj.SelectTokens(Query)
            // NOTE: we cannot create items with empty ItemSpec, so skip them entirely.
            // see https://github.com/dotnet/msbuild/issues/3399
            .Where(x => !string.IsNullOrEmpty(x.ToString()))
            .SelectMany(x => x.AsItems()).ToArray();

        return true;
    }

    class ExpandoObjectFactory : IObjectFactory
    {
        readonly IObjectFactory fallback = new DefaultObjectFactory();

        public static IObjectFactory Default { get; } = new ExpandoObjectFactory();

        ExpandoObjectFactory() { }

        public object Create(Type type) => type switch
        {
            _ when type == typeof(IDictionary) => new ExpandoObject(),
            _ when type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>) => new ExpandoObject(),
            _ => fallback.Create(type)!,
        };
    }
}
