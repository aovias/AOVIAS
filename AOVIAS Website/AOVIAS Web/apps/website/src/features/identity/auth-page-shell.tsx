import type { ReactNode } from 'react';
import Link from 'next/link';

import { Heading, Paragraph, Stack, Text } from '@/components/primitives';

export type AuthPageLink = {
  href: string;
  label: string;
};

export type AuthPageShellProps = {
  title: string;
  description?: string;
  children: ReactNode;
  /** Supporting links below the form (forgot password, sign up, etc.). */
  links?: readonly AuthPageLink[];
  footerNote?: string;
};

/**
 * Shared authentication page chrome — title, form slot, supporting links.
 */
export function AuthPageShell({
  title,
  description,
  children,
  links,
  footerNote,
}: AuthPageShellProps) {
  return (
    <Stack gap={8}>
      <Stack gap={3}>
        <Heading level={1} size="h3">
          {title}
        </Heading>
        {description ? (
          <Paragraph size="small" tone="muted">
            {description}
          </Paragraph>
        ) : null}
      </Stack>

      {children}

      {links && links.length > 0 ? (
        <nav aria-label="Account options">
          <ul className="flex list-none flex-col gap-2 p-0">
            {links.map((link) => (
              <li key={link.href}>
                <Link
                  href={link.href}
                  className="text-primary text-small underline underline-offset-4"
                >
                  {link.label}
                </Link>
              </li>
            ))}
          </ul>
        </nav>
      ) : null}

      {footerNote ? (
        <Text as="p" size="caption" tone="muted">
          {footerNote}
        </Text>
      ) : null}
    </Stack>
  );
}
