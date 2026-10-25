using Aovias.Deployment.Contracts;
using Aovias.Deployment.Contracts.Provisioning;

namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Ensures certificates declared by deployment contracts exist under the runtime config mount.
/// </summary>
public static class IdentitySigningCertificateProvisioner
{
    public static void EnsureProvisioned(string configRoot, CertificateContract? contract = null)
    {
        if (contract is null || contract.Certificates.Count == 0)
        {
            return;
        }

        ContractCertificateProvisioner.EnsureProvisioned(configRoot, contract);
    }
}
