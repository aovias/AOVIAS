import { MarketingCtaLink, SectionHeader } from '@/components/marketing';
import {
  Badge,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Container,
  Stack,
  Text,
} from '@/components/primitives';
import {
  employmentTypeLabels,
  getOpenRoles,
  roleStatusLabels,
  type OpenRole,
  type RoleStatus,
} from '@/features/careers/domain';
import {
  openRolesContent,
  openRolesMeta,
} from '@/features/careers/open-roles/content';

const statusVariant: Record<
  RoleStatus,
  'success' | 'info' | 'secondary' | 'default'
> = {
  open: 'success',
  upcoming: 'info',
  filled: 'secondary',
  closed: 'default',
};

function RoleCard({ role }: { role: OpenRole }) {
  const canApply = role.status === 'open';

  return (
    <Card id={`role-${role.slug}`} className="h-full">
      <CardHeader>
        <Stack gap={3}>
          <Stack direction="horizontal" gap={2} wrap align="center">
            <Badge variant={statusVariant[role.status]}>
              {roleStatusLabels[role.status]}
            </Badge>
            <Badge variant="outline">{role.department}</Badge>
            <Badge variant="secondary">
              {employmentTypeLabels[role.employmentType]}
            </Badge>
          </Stack>
          <CardTitle>{role.title}</CardTitle>
          <Text as="p" size="caption" tone="muted">
            {role.location}
          </Text>
          <CardDescription>{role.summary}</CardDescription>
        </Stack>
      </CardHeader>
      <CardContent>
        {canApply ? (
          <MarketingCtaLink
            label="Apply"
            href={role.applyHref}
            variant="primary"
            size="md"
          />
        ) : (
          <Text as="p" size="small" tone="muted">
            Applications open when this role moves to Open status.
          </Text>
        )}
      </CardContent>
    </Card>
  );
}

/**
 * Open Roles — configuration-driven listings from the careers domain registry.
 */
export function OpenRolesSection() {
  const roles = getOpenRoles();

  return (
    <section
      id={openRolesMeta.sectionId}
      className="w-full py-16 lg:py-20"
      aria-label={openRolesMeta.title}
    >
      <Container width="wide">
        <Stack gap={10}>
          <SectionHeader
            eyebrow={openRolesContent.eyebrow}
            heading={openRolesContent.heading}
            description={openRolesContent.description}
          />

          {roles.length === 0 ? (
            <div
              role="status"
              className="border-border bg-muted/30 rounded-lg border border-dashed px-6 py-10 text-center"
            >
              <Text as="p" weight="medium">
                {openRolesContent.emptyTitle}
              </Text>
              <Text as="p" size="small" tone="muted" className="mt-2">
                {openRolesContent.emptyDescription}
              </Text>
              <div className="mt-6 flex justify-center">
                <MarketingCtaLink
                  label={openRolesContent.generalApplyLabel}
                  href={openRolesContent.generalApplyHref}
                  variant="secondary"
                  size="md"
                />
              </div>
            </div>
          ) : (
            <ul className="grid list-none gap-6 p-0 md:grid-cols-2 lg:grid-cols-3">
              {roles.map((role) => (
                <li key={role.id}>
                  <RoleCard role={role} />
                </li>
              ))}
            </ul>
          )}
        </Stack>
      </Container>
    </section>
  );
}
