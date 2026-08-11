using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.RepresentationModel;

namespace Aovias.Publishing.Core.Generation.Compose;

/// <summary>
/// Serializes a <see cref="ComposeDocument"/> to deterministic YAML using the
/// YamlDotNet representation model (insertion order preserved, no version field).
/// </summary>
public static class ComposeYamlSerializer
{
    public static string Serialize(ComposeDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var root = new YamlMappingNode();

        var services = new YamlMappingNode();
        foreach (var service in document.Services)
        {
            services.Add(Plain(service.Name), BuildService(service));
        }

        root.Add(Plain("services"), services);

        if (document.Networks.Count > 0)
        {
            var networks = new YamlMappingNode();
            foreach (var network in document.Networks)
            {
                networks.Add(Plain(network.Alias), BuildNetwork(network));
            }

            root.Add(Plain("networks"), networks);
        }

        var stream = new YamlStream(new YamlDocument(root));
        using var writer = new StringWriter();
        stream.Save(writer, assignAnchors: false);

        // YamlStream emits a trailing document-end marker ("...") which is valid
        // but noisy for compose; strip it for clean, deterministic output.
        var yaml = writer.ToString().Replace("...\n", string.Empty, StringComparison.Ordinal).TrimEnd();
        return yaml + "\n";
    }

    private static YamlMappingNode BuildService(ComposeService service)
    {
        var node = new YamlMappingNode
        {
            { Plain("image"), Quoted(service.Image) },
            { Plain("container_name"), Quoted(service.ContainerName) },
            { Plain("restart"), Plain(service.Restart) }
        };

        if (service.Ports.Count > 0)
        {
            node.Add(Plain("ports"), Sequence(service.Ports, quoted: true));
        }

        if (service.Environment.Count > 0)
        {
            var environment = new YamlMappingNode();
            foreach (var pair in service.Environment)
            {
                environment.Add(Plain(pair.Key), Quoted(pair.Value));
            }

            node.Add(Plain("environment"), environment);
        }

        if (service.Volumes.Count > 0)
        {
            node.Add(Plain("volumes"), Sequence(service.Volumes, quoted: true));
        }

        if (service.NetworkAliases.Count > 0)
        {
            node.Add(Plain("networks"), Sequence(service.NetworkAliases, quoted: false));
        }

        if (service.Healthcheck is not null)
        {
            node.Add(Plain("healthcheck"), BuildHealthcheck(service.Healthcheck));
        }

        if (service.Labels.Count > 0)
        {
            var labels = new YamlMappingNode();
            foreach (var pair in service.Labels)
            {
                labels.Add(Plain(pair.Key), Quoted(pair.Value));
            }

            node.Add(Plain("labels"), labels);
        }

        return node;
    }

    private static YamlMappingNode BuildHealthcheck(ComposeHealthcheck healthcheck)
    {
        var test = new YamlSequenceNode();
        test.Style = SequenceStyle.Flow;
        foreach (var item in healthcheck.Test)
        {
            test.Add(Quoted(item));
        }

        return new YamlMappingNode
        {
            { Plain("test"), test },
            { Plain("interval"), Plain(healthcheck.Interval) },
            { Plain("timeout"), Plain(healthcheck.Timeout) },
            { Plain("retries"), Plain(healthcheck.Retries.ToString()) },
            { Plain("start_period"), Plain(healthcheck.StartPeriod) }
        };
    }

    private static YamlMappingNode BuildNetwork(ComposeNetwork network)
    {
        return new YamlMappingNode
        {
            { Plain("name"), Quoted(network.Name) },
            { Plain("external"), Plain(network.External ? "true" : "false") }
        };
    }

    private static YamlSequenceNode Sequence(IEnumerable<string> values, bool quoted)
    {
        var sequence = new YamlSequenceNode();
        foreach (var value in values)
        {
            sequence.Add(quoted ? Quoted(value) : Plain(value));
        }

        return sequence;
    }

    private static YamlScalarNode Plain(string value) => new(value) { Style = ScalarStyle.Plain };

    private static YamlScalarNode Quoted(string value) => new(value) { Style = ScalarStyle.DoubleQuoted };
}
