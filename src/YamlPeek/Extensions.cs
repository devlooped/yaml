using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json.Linq;
using SharpYaml.Serialization;

static class Extensions
{
    static readonly Serializer serializer = new(new SerializerSettings
    {
        //ObjectFactory = ExpandoObjectFactory.Default,
        EmitAlias = false,
        EmitTags = false,
        ComparerForKeySorting = null,
    });

    public static bool Error(this TaskLoggingHelper log, string code, string message, bool result = false)
    {
        log.LogError(null, code, null, null, 0, 0, 0, 0, message);
        return result;
    }

    public static bool Warn(this TaskLoggingHelper log, string code, string message, bool result = false)
    {
        log.LogWarning(null, code, null, null, 0, 0, 0, 0, message);
        return result;
    }

    public static IEnumerable<ITaskItem> AsItems(this JToken json) => json switch
    {
        JObject complex => new ITaskItem[] { complex.AsItem() },
        JArray array => array.SelectMany(AsItems),
        { Type: JTokenType.Null } => Array.Empty<ITaskItem>(),
        _ => new ITaskItem[] { new TaskItem(json.AsString()) },
    };

    public static string AsString(this JToken token) => token.AsObject() switch
    {
        string value => value,
        bool value => value.ToString().ToLowerInvariant(),
        object value when value.GetType().IsValueType => value.ToString(),
        object value => serializer.Serialize(value).TrimEnd('\r', '\n'),
        _ => "",
    };

    static object? AsObject(this JToken token) => token switch
    {
        _ when token.Type == JTokenType.String => token.ToString(),
        JValue primitive => primitive.Value,
        JArray array => array.Select(AsObject).ToArray(),
        JObject complex => complex.ToDictionary(),
        _ => token.ToString(),
    };

    static object ToDictionary(this JObject complex)
    {
        // We don't use an ExpandoObject here because it's internally backed by 
        // a dictionary, which changes the order of keys and would change the 
        // original yaml in potentially undesirable ways.
        var value = new OrderedDictionary();
        foreach (var prop in complex.OfType<JProperty>())
            value.Add(prop.Name, prop.Value.AsObject());

        return value;
    }

    static ITaskItem AsItem(this JObject json)
    {
        var item = new TaskItem(json.Path);
        // Top-level properties turned into metadata for convenience.
        foreach (var prop in json.Properties())
            item.SetMetadata(prop.Name, prop.Value.AsString());

        item.SetMetadata("_", json.AsString());

        return item;
    }
}
