![Yaml Icon](https://raw.githubusercontent.com/devlooped/yaml/main/assets/img/icon.png) YamlPeek MSBuild Task
============

[![Version](https://img.shields.io/nuget/vpre/YamlPeek.svg?color=royalblue)](https://www.nuget.org/packages/YamlPeek)
[![Downloads](https://img.shields.io/nuget/dt/YamlPeek.svg?color=green)](https://www.nuget.org/packages/YamlPeek)
[![License](https://img.shields.io/github/license/devlooped/yaml.svg?color=blue)](https://github.com/devlooped/yaml/blob/main/license.txt)
[![Build](https://github.com/devlooped/yaml/workflows/build/badge.svg?branch=main)](https://github.com/devlooped/yaml/actions)

Read values from yaml files using JSONPath query expressions.


Usage:

```xml
  <YamlPeek ContentPath="[YAML_FILE]" Query="[JSONPath]">
    <Output TaskParameter="Result" PropertyName="Value" />
  </YamlPeek>
  <YamlPeek Content="[YAML]" Query="[JSONPath]">
    <Output TaskParameter="Result" ItemName="Values" />
  </YamlPeek>
```

Parameters:

| Parameter   | Description                                                                                                    |
| ----------- | -------------------------------------------------------------------------------------------------------------- |
| Content     | Optional `string` parameter.<br/>Specifies the YAML input as a string.                           |
| ContentPath | Optional `ITaskItem` parameter.<br/>Specifies the YAML input as a file path.                                   |
| Query       | Required `string` parameter.<br/>Specifies the [JSONPath](https://goessner.net/articles/JsonPath/) expression. |
| Result      | Output `ITaskItem[]` parameter.<br/>Contains the results that are returned by the task.                        |

You can either provide the path to a YAML file via `ContentPath` or 
provide the straight YAML content to `Content`. The `Query` is a 
[JSONPath](https://goessner.net/articles/JsonPath/) expression that is evaluated 
and returned via the `Result` task parameter. You can assign the resulting 
value to either a property (i.e. for a single value) or an item name (i.e. 
for multiple results).

YAML object properties are automatically projected as item metadata when 
assigning the resulting value to an item. For example, given the following JSON:

```yaml
http:
  host: localhost
  port: 80
  ssl: true
```

You can read the entire `http` value as an item with each property as a metadata 
value with:

```xml
<YamlPeek ContentPath="host.yaml" Query="$.http">
    <Output TaskParameter="Result" ItemName="Http" />
</YamlPeek>
```

The `Http` item will have the following values (if it were declared in MSBuild):

```xml
<ItemGroup>
    <Http Include="[item raw json]">
        <host>localhost</host>
        <port>80</port>
        <ssl>true</ssl>
    </Http>
</ItemGroup>
```

These item metadata values could be read as MSBuild properties as follows, for example:

```xml
<PropertyGroup>
    <Host>@(Http -> '%(host)')</Host>
    <Port>@(Http -> '%(port)')</Port>
    <Ssl>@(Http -> '%(ssl)')</Ssl>
</PropertyGroup>
```

In addition to the explicitly opted in object properties, the entire node is available 
as raw YAML via the special `_` (single underscore) metadata item.
