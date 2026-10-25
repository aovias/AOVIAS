'use client';

import { useId, useState } from 'react';

import { cn } from '@/lib/utils';

/**
 * Switch — accessible toggle foundation (button role=switch).
 */
export type SwitchProps = {
  checked?: boolean;
  defaultChecked?: boolean;
  onCheckedChange?: (checked: boolean) => void;
  disabled?: boolean;
  className?: string;
  id?: string;
  'aria-label'?: string;
  'aria-labelledby'?: string;
};

export function Switch({
  checked,
  defaultChecked = false,
  onCheckedChange,
  disabled = false,
  className,
  id,
  'aria-label': ariaLabel,
  'aria-labelledby': ariaLabelledBy,
}: SwitchProps) {
  const generatedId = useId();
  const switchId = id ?? generatedId;
  const isControlled = checked !== undefined;
  const [uncontrolled, setUncontrolled] = useState(defaultChecked);
  const isOn = isControlled ? Boolean(checked) : uncontrolled;

  return (
    <button
      id={switchId}
      type="button"
      role="switch"
      aria-checked={isOn}
      aria-label={ariaLabel}
      aria-labelledby={ariaLabelledBy}
      disabled={disabled}
      data-state={isOn ? 'checked' : 'unchecked'}
      className={cn(
        'focus-visible:ring-ring relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50',
        isOn ? 'bg-primary' : 'bg-muted',
        className,
      )}
      onClick={() => {
        if (disabled) return;
        const next = !isOn;
        if (!isControlled) setUncontrolled(next);
        onCheckedChange?.(next);
      }}
    >
      <span
        aria-hidden="true"
        className={cn(
          'bg-background pointer-events-none inline-block size-4 rounded-full shadow-sm transition-transform',
          isOn ? 'translate-x-5' : 'translate-x-0.5',
        )}
      />
    </button>
  );
}
