using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xunit;
using Xunit.Abstractions;

public record Tests(ITestOutputHelper Output)
{
    [Theory]
    [MemberData(nameof(GetTargets))]
    public void Run(string file, string name, bool failure = false, string? code = null)
    {
        var logger = new TestLogger();
        var result = BuildManager.DefaultBuildManager.Build(
            new BuildParameters
            {
                ResetCaches = true,
                Loggers = new ILogger[] { logger }
            },
            new BuildRequestData(
                Path.Combine(Directory.GetCurrentDirectory(), file),
                new Dictionary<string, string>(), null, new[] { name }, null));

        if (failure)
        {
            if (result.OverallResult != BuildResultCode.Failure)
                Output.WriteLine(string.Join(Environment.NewLine, logger.Events
                    .Select(e => e.Message)));

            Assert.Equal(BuildResultCode.Failure, result.OverallResult);
            Assert.Contains(code, logger.Errors);
        }
        else
        {
            if (result.OverallResult != BuildResultCode.Success)
                Output.WriteLine(string.Join(Environment.NewLine, logger.Events
                    .Select(e => e.Message)));

            Assert.Equal(BuildResultCode.Success, result.OverallResult);
            if (code != null)
                Assert.Contains(code, logger.Warnings);
        }

        Output.WriteLine(string.Join(Environment.NewLine, logger.Events.OfType<BuildMessageEventArgs>()
            .Where(e => e.Importance == MessageImportance.High)
            .Select(e => e.Message)));
    }

    public static IEnumerable<object?[]> GetTargets()
    {
        foreach (var file in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.targets"))
        {
            foreach (var target in XDocument.Load(file).Root!.Elements("Target"))
            {
                var label = target.Attribute("Label")?.Value;
                var name = target.Attribute("Name")!.Value;
                if (label == null)
                {
                    yield return new object?[] { Path.GetFileName(file), name, };
                    continue;
                }

                var parts = label.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    continue;

                yield return new object[] { Path.GetFileName(file), name, parts[0] == "Error", parts[1] };
            }
        }
    }

    class TestLogger : Logger
    {
        public HashSet<string> Warnings { get; } = new();
        public HashSet<string> Errors { get; } = new();
        public List<BuildEventArgs> Events { get; } = new();

        public override void Initialize(IEventSource eventSource)
        {
            eventSource.AnyEventRaised += (_, e) => Events.Add(e);
            eventSource.ErrorRaised += (_, e) => Errors.Add(e.Code);
            eventSource.WarningRaised += (_, e) => Warnings.Add(e.Code);
        }
    }
}