using Duende.IdentityServer.Models;

namespace IdentityService.Config
{
    public static class IdentityConfig
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile()
            };

        public static IEnumerable<ApiScope> ApiScopes =>
            new[]
            {
            new ApiScope("organization.api", "Organization Service API"),
            new ApiScope("compliance.api", "Compliance Service API"),
            new ApiScope("audit.api", "Audit Service API"),
            new ApiScope("ai.api", "AI Orchestrator Service API"),
            };

        public static IEnumerable<ApiResource> ApiResources =>
            new[]
            {
            new ApiResource("organization.api")
            {
                Scopes = { "organization.api" }
            },
            new ApiResource("compliance.api")
            {
                Scopes = { "compliance.api" }
            },
            new ApiResource("audit.api")
            {
                Scopes = { "audit.api" }
            },
            new ApiResource("ai.api")
            {
                Scopes = { "ai.api" }
            }
            };

        public static IEnumerable<Client> Clients =>
            new[]
            {
            // SPA Client
            new Client
            {
                ClientId = "spa-client",
                AllowedGrantTypes = GrantTypes.Code,
                RequireClientSecret = false,
                RedirectUris = { "https://localhost:4200/auth-callback" },
                AllowedScopes = { "openid", "profile", "organization.api", "compliance.api", "audit.api", "ai.api" },
                AllowOfflineAccess = true
            },

            // Postman / API Client
            new Client
            {
                ClientId = "postman-client",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                ClientSecrets = { new Secret("postman-secret".Sha256()) },
                AllowedScopes = { "openid", "profile", "organization.api", "compliance.api", "audit.api", "ai.api" }
            },

            // Machine-to-machine
            new Client
            {
                ClientId = "organization-service",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("org-secret".Sha256()) },
                AllowedScopes = { "organization.api" }
            }
            };
    }
}
