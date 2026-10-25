'use client';

import type { ReactNode } from 'react';

import { Label, Stack } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * Field — accessible label + control + description + error shell.
 * All typed controls compose through this wrapper.
 */
export type FieldProps = {
  id: string;
  label?: string;
  description?: string;
  error?: string;
  required?: boolean;
  className?: string;
  children: ReactNode;
  /** When true, label wraps the control (checkbox/switch). */
  inline?: boolean;
};

export function Field({
  id,
  label,
  description,
  error,
  required,
  className,
  children,
  inline = false,
}: FieldProps) {
  const descriptionId = description ? `${id}-description` : undefined;
  const errorId = error ? `${id}-error` : undefined;

  if (inline) {
    return (
      <div className={cn('w-full', className)}>
        <div className="flex items-start gap-3">
          <div className="pt-0.5">{children}</div>
          <Stack gap={1} className="min-w-0">
            {label ? (
              <Label htmlFor={id} {...(required ? { required: true } : {})}>
                {label}
              </Label>
            ) : null}
            {description ? (
              <p id={descriptionId} className="text-muted-foreground text-caption">
                {description}
              </p>
            ) : null}
            {error ? (
              <p id={errorId} role="alert" className="text-error text-caption">
                {error}
              </p>
            ) : null}
          </Stack>
        </div>
      </div>
    );
  }

  return (
    <Stack gap={2} className={cn('w-full', className)}>
      {label ? (
        <Label htmlFor={id} {...(required ? { required: true } : {})}>
          {label}
        </Label>
      ) : null}
      {children}
      {description ? (
        <p id={descriptionId} className="text-muted-foreground text-caption">
          {description}
        </p>
      ) : null}
      {error ? (
        <p id={errorId} role="alert" className="text-error text-caption">
          {error}
        </p>
      ) : null}
    </Stack>
  );
}

export function fieldA11yProps(
  id: string,
  error?: string,
  description?: string,
  required?: boolean,
) {
  const descriptionId = description ? `${id}-description` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy =
    [descriptionId, errorId].filter(Boolean).join(' ') || undefined;

  return {
    id,
    ...(error ? { 'aria-invalid': true as const } : {}),
    ...(required ? { 'aria-required': true as const } : {}),
    ...(describedBy ? { 'aria-describedby': describedBy } : {}),
  };
}
