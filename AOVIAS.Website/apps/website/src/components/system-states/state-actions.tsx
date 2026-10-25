import { MarketingCtaLink } from '@/components/marketing';
import type { StateAction } from '@/components/system-states/types';
import { Button, Stack } from '@/components/primitives';

type StateActionsProps = {
  primary?: StateAction;
  secondary?: StateAction;
  primaryVariant?: 'primary' | 'outline' | 'secondary';
};

function ActionControl({
  action,
  variant,
}: {
  action: StateAction;
  variant: 'primary' | 'outline' | 'secondary';
}) {
  if (action.href) {
    return (
      <MarketingCtaLink
        label={action.label}
        href={action.href}
        {...(action.external ? { external: true } : {})}
        variant={variant}
      />
    );
  }

  if (action.onClick) {
    return (
      <Button type="button" variant={variant} onClick={action.onClick}>
        {action.label}
      </Button>
    );
  }

  return null;
}

/**
 * Renders state CTAs as links (server-friendly) or buttons (client parents).
 * Mark parent `'use client'` when using onClick handlers.
 */
export function StateActions({
  primary,
  secondary,
  primaryVariant = 'primary',
}: StateActionsProps) {
  if (!primary && !secondary) return null;

  return (
    <Stack direction="horizontal" gap={3} wrap className="justify-center">
      {primary ? (
        <ActionControl action={primary} variant={primaryVariant} />
      ) : null}
      {secondary ? (
        <ActionControl action={secondary} variant="outline" />
      ) : null}
    </Stack>
  );
}
