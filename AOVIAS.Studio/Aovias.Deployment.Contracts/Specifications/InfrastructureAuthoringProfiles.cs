namespace Aovias.Deployment.Contracts.Specifications;

public static class InfrastructureAuthoringProfiles
{
    public static ExpandedInfrastructureSpecification Expand(InfrastructureSpecification specification)
    {
        var d = specification.Profile.ToLowerInvariant() switch
        {
            "http-api" => ("standard", "none", "none", "jwt", "none", "standard", "standard"),
            "worker-service" => ("standard", "none", "none", "none", "none", "worker", "standard"),
            "static-web" => ("static", "none", "none", "none", "portal", "static", "static"),
            "runtime-agent" => ("runtime-agent", "none", "none", "none", "runtime-agent", "runtime-agent", "runtime-agent"),
            _ => throw new InfrastructureSpecificationException([$"Unknown authoring profile '{specification.Profile}'."])
        };
        return new ExpandedInfrastructureSpecification
        {
            ServiceId = specification.ServiceId, Profile = specification.Profile,
            Filesystem = Filesystem(specification.Filesystem ?? d.Item1),
            Certificates = Certificates(specification.Certificates ?? d.Item2),
            Database = Database(specification.Database ?? d.Item3),
            Secrets = Secrets(specification.Secrets ?? d.Item4),
            Configuration = Configuration(specification.Configuration ?? d.Item5),
            Health = Health(specification.Health ?? d.Item6),
            Network = new NetworkContract { NetworkNameToken = "${AOVIAS_DOCKER_NETWORK}", ExternalNetwork = true },
            Volumes = Volumes(specification.Volumes ?? d.Item7),
            Features = specification.Features, Runtime = specification.Runtime,
            Providers = specification.Providers, PublishedDependencies = specification.PublishedDependencies,
            Requirements = specification.Requirements
        };
    }

    private static FilesystemContract Filesystem(string name)
    {
        var writable = new List<FilesystemDirectoryContract> { Dir(PlatformRuntimeDefaults.LogsRoot, "logs"), Dir(PlatformRuntimeDefaults.PackagesRoot, "packages") };
        var readOnly = new List<FilesystemDirectoryContract> { Dir(PlatformRuntimeDefaults.ConfigRoot, "config", "755") };
        switch (name)
        {
            case "standard": break;
            case "certificates": readOnly.Add(Dir(PlatformRuntimeDefaults.CertificatesRoot, "certificates", "755")); break;
            case "evidence":
                writable.Add(Dir(EvidenceUploadsContainerPath, "evidence"));
                readOnly.Add(Dir(PlatformRuntimeDefaults.CertificatesRoot, "certificates", "755")); break;
            case "static": writable.RemoveAll(x => x.Path == PlatformRuntimeDefaults.PackagesRoot); break;
            case "runtime-agent":
                readOnly.Add(Dir(PlatformRuntimeDefaults.CertificatesRoot, "certificates", "755"));
                foreach (var (_, path) in PlatformDeploymentDefaults.RequiredWritableResources)
                    writable.Add(Dir(path, Path.GetFileName(path.TrimEnd('/'))));
                break;
            default: throw Unknown("filesystem", name);
        }
        return new FilesystemContract { RequiredUser = PlatformRuntimeDefaults.ContainerUserId, RequiredGroup = PlatformRuntimeDefaults.ContainerUserId, Directories = writable, ReadOnlyDirectories = readOnly };
    }

    private static CertificateContract Certificates(string name) => name switch
    {
        "none" => new CertificateContract(),
        "identity-signing" => new CertificateContract { Certificates = [new CertificateRequirementContract { Id = "identity-signing", Required = true, Type = "PFX", Category = "Signing", MountPath = PlatformRuntimeDefaults.CertificatesRoot, FileName = "identity-signing.pfx", SecretKey = "certificatePassword" }] },
        _ => throw Unknown("certificate", name)
    };

    private static DatabaseContract Database(string name) => name switch
    {
        "none" => new DatabaseContract { Provider = "None", Required = false, Migration = "None", Seed = "None" },
        "sql-auto-dev" => Sql("Automatic", "DevelopmentOnly"),
        "sql-dev-dev" => Sql("DevelopmentOnly", "DevelopmentOnly"),
        "sql-dev-always" => Sql("DevelopmentOnly", "Always"),
        "sql-auto-none" => Sql("Automatic", "None"),
        "sql-dev-none" => Sql("DevelopmentOnly", "None"),
        "outbox" => new DatabaseContract { Provider = "SqlServer", Required = true, ConnectionSetting = "ConnectionStrings:ComplianceConnection", Migration = "None", Seed = "None", HealthCheck = false, StartupPolicy = "Required" },
        _ => throw Unknown("database", name)
    };
    private static DatabaseContract Sql(string migration, string seed) => new() { Provider = "SqlServer", Required = true, ConnectionSetting = "ConnectionStrings:DefaultConnection", Migration = migration, Seed = seed, HealthCheck = true, StartupPolicy = "Required", RollbackPolicy = "Manual" };

    private static SecretContract Secrets(string name)
    {
        var s = new List<SecretRequirementContract>();
        if (name is "jwt" or "sql" or "identity" or "compliance" or "ai")
            s.AddRange([Secret("jwt-secret", "jwtSigningKey", "JwtSettings:SecretKey"), Secret("jwt-issuer", "jwtIssuer", "JwtSettings:Issuer", false), Secret("jwt-audience", "jwtAudience", "JwtSettings:Audience", false)]);
        if (name is "sql" or "identity" or "compliance" or "ai") s.Add(Secret("database-connection", "databaseConnectionString", "ConnectionStrings:DefaultConnection"));
        if (name == "identity") s.Add(Secret("certificate-password", "certificatePassword", "Identity:Signing:CertificatePassword"));
        if (name == "compliance") s.AddRange([Secret("rabbitmq-host", "RabbitMQ:HostName", "RabbitMQ:HostName"), Secret("rabbitmq-password", "RabbitMQ:Password", "RabbitMQ:Password")]);
        if (name == "ai") s.AddRange([Secret("azure-openai-key", "AzureOpenAI:ApiKey", "AzureOpenAI:ApiKey", false), Secret("sarvam-api-key", "Sarvam:ApiKey", "Sarvam:ApiKey", false), Secret("service-bus", "AIEventProcessing:ServiceBusConnectionString", "AIEventProcessing:ServiceBusConnectionString", false)]);
        if (name == "outbox") s.AddRange([Secret("database-connection", "databaseConnectionString", "ConnectionStrings:ComplianceConnection"), Secret("service-bus", "ConnectionStrings:ServiceBus", "ConnectionStrings:ServiceBus")]);
        if (name is not ("none" or "jwt" or "sql" or "identity" or "compliance" or "ai" or "outbox")) throw Unknown("secret", name);
        return new SecretContract { Secrets = s };
    }

    private static ConfigurationContract Configuration(string name) => name switch
    {
        "none" => new ConfigurationContract(),
        "runtime-agent" => new ConfigurationContract { Required = [new ConfigurationRequirementContract { Key = "AOVIAS_CONFIG_ROOT", DefaultValue = PlatformRuntimeDefaults.ConfigRoot }, new ConfigurationRequirementContract { Key = "AOVIAS_ACTIVE_PROFILE", ValidationRule = "non-empty" }] },
        "portal" => new ConfigurationContract { Required = [new ConfigurationRequirementContract { Key = "PRAMAN_API_BASE_URL", ValidationRule = "absolute-uri" }] },
        "outbox" => new ConfigurationContract { Required = [new ConfigurationRequirementContract { Key = "ConnectionStrings:ComplianceConnection", ValidationRule = "non-empty" }] },
        _ => throw Unknown("configuration", name)
    };

    private static HealthContract Health(string name)
    {
        var h = new HealthContract { HealthUrl = "/health", ReadinessUrl = "/health/ready", LivenessUrl = "/health", ExpectedStatus = 200, TimeoutSeconds = 15, Retries = 3, StabilizationSeconds = 15, StartupTimeoutSeconds = 120, ShutdownTimeoutSeconds = 30, RequiresHttpProbe = true };
        return name switch
        {
            "standard" => h,
            "static" => h with { Retries = 2 },
            "worker" => h with { RequiresHttpProbe = false, HealthUrl = string.Empty },
            "runtime-agent" => h with { StabilizationSeconds = 20, StartupTimeoutSeconds = 180, AdditionalProbes = [Probe("GET", "/api/runtime/health"), Probe("GET", "/api/runtime/version"), Probe("GET", "/api/runtime/environment"), Probe("GET", "/api/runtime/capabilities"), Probe("GET", "/api/runtime/workflows"), Probe("GET", "/api/runtime/releases"), Probe("POST", "/hubs/deployment/negotiate?negotiateVersion=1"), Probe("GET", "/api/deployment/status", true), Probe("GET", "/api/deployment/history", true)] },
            _ => throw Unknown("health", name)
        };
    }

    /// <summary>Container path for the "evidence" filesystem profile's writable upload directory — shared with <see cref="Volumes"/> so the filesystem-declared writable directory and its bind mount can never drift apart.</summary>
    private const string EvidenceUploadsContainerPath = "/app/App_Data/evidence_uploads";

    private static VolumeContract Volumes(string name)
    {
        var m = new List<VolumeMountContract> { Mount("${AOVIAS_HOST_CONFIG_ROOT}", PlatformRuntimeDefaults.ConfigRoot, true), Mount("${AOVIAS_HOST_LOGS_ROOT}", PlatformRuntimeDefaults.LogsRoot) };
        if (name is "standard" or "runtime-agent" or "evidence") m.Add(Mount("${AOVIAS_HOST_PACKAGES_ROOT}", PlatformRuntimeDefaults.PackagesRoot));
        if (name == "runtime-agent") m.Add(Mount(PlatformDeploymentDefaults.HostDeploymentsRootToken, PlatformDeploymentDefaults.DeploymentRoot));
        if (name == "evidence") m.Add(Mount("${AOVIAS_HOST_EVIDENCE_ROOT}", EvidenceUploadsContainerPath));
        if (name is not ("standard" or "runtime-agent" or "static" or "evidence")) throw Unknown("volume", name);
        return new VolumeContract { Mounts = m };
    }

    private static SecretRequirementContract Secret(string id, string name, string key, bool required = true) => new() { Id = id, Name = name, Required = required, ConfigurationKey = key };
    private static FilesystemDirectoryContract Dir(string path, string purpose, string permission = "775") => new() { Path = path, Permission = permission, Owner = PlatformRuntimeDefaults.ContainerUserId.ToString(), Purpose = purpose };
    private static VolumeMountContract Mount(string host, string container, bool readOnly = false) => new() { HostToken = host, ContainerPath = container, ReadOnly = readOnly };
    private static HealthProbeEndpointContract Probe(string method, string path, bool required = false) => new() { Method = method, Path = path, ExpectedStatus = 200, Required = required };
    private static InfrastructureSpecificationException Unknown(string domain, string name) => new([$"Unknown {domain} template '{name}'."]);
}