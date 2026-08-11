/**
 * The single declaration of what "the AOVIAS stack" is.
 *
 * Ports and launch profiles are recorded here because RC2 lost a wave to them: after a machine
 * restart, OrganizationService and PersonService came up on their HTTP ports (5085 / 5190) instead
 * of the HTTPS ports the Gateway targets (7181 / 7190). Every /health probe passed and the stack
 * still did not work, because the Gateway could not reach either service. `launchProfile` and
 * `wrongPortSymptom` exist so that failure is reported, not rediscovered.
 */

export const APP_BASE_URL = 'http://localhost:4200';
export const ARTIFACT_ROOT = 'artifacts';

export const DATABASE = {
  server: 'localhost\\SQLEXPRESS',
  database: 'Compliance',
  trusted: true,
} as const;

export interface ServiceDefinition {
  /** Business name, used in diagnostics. */
  readonly name: string;
  /** Project path relative to the repository root. */
  readonly project: string;
  /** Scheme + port the Gateway and clients expect. */
  readonly healthUrl: string;
  /** `dotnet run --launch-profile <x>`; omitted where the default profile is correct. */
  readonly launchProfile?: string;
  /** Ports this service binds when launched with the WRONG profile — probed to explain failures. */
  readonly wrongPortSymptom?: readonly string[];
  /** Gateway cannot serve the app without these. */
  readonly required: boolean;
}

const PLATFORM_ROOT = 'ComplaincePlatform';

export const SERVICES: readonly ServiceDefinition[] = [
  {
    name: 'IdentityService',
    project: `${PLATFORM_ROOT}/IdentityService/IdentityService.csproj`,
    healthUrl: 'http://localhost:5062/health',
    required: true,
  },
  {
    name: 'OrganizationService',
    project: `${PLATFORM_ROOT}/OrganizationService/OrganizationService.csproj`,
    healthUrl: 'https://localhost:7181/health',
    launchProfile: 'https',
    wrongPortSymptom: ['http://localhost:5085/health'],
    required: true,
  },
  {
    name: 'ComplianceService',
    project: `${PLATFORM_ROOT}/ComplianceService/ComplianceService.csproj`,
    healthUrl: 'http://localhost:5016/health',
    required: true,
  },
  {
    name: 'PersonService',
    project: `${PLATFORM_ROOT}/PersonService/PersonService.csproj`,
    healthUrl: 'https://localhost:7190/health',
    launchProfile: 'https',
    wrongPortSymptom: ['http://localhost:5190/health'],
    required: true,
  },
  {
    name: 'AccessGovernanceService',
    project: `${PLATFORM_ROOT}/AccessGovernanceService/AccessGovernanceService.csproj`,
    healthUrl: 'https://localhost:7205/health',
    required: true,
  },
  {
    name: 'Gateway',
    project: `${PLATFORM_ROOT}/Gateway/Gateway.csproj`,
    healthUrl: 'http://localhost:5212/health',
    required: true,
  },
] as const;

/** Services whose stdout the Evidence Collector tails. */
export const LOGGED_SERVICES = ['ComplianceService', 'Gateway'] as const;

/**
 * Seed data the platform cannot function without. Verified through the running application, never
 * by querying tables — the certification platform must not know the schema.
 */
export const REQUIRED_SEED = {
  /** At least one audit type must be startable, or an auditor cannot open a session (DEF-RC2-001). */
  startableAuditTypes: 1,
  /** At least one framework must exist to select during context selection. */
  frameworks: 1,
} as const;
