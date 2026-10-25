import { cn } from '@/lib/utils';

export type SidebarPlaceholderProps = {
  className?: string;
  label?: string;
};

/** Structural sidebar slot — no production content in Sprint 1.3. */
export function SidebarPlaceholder({
  className,
  label = 'Sidebar',
}: SidebarPlaceholderProps) {
  return (
    <aside
      aria-label={label}
      className={cn(
        'border-border bg-muted/40 hidden w-64 shrink-0 rounded-md border border-dashed p-4 lg:block',
        className,
      )}
    >
      <p className="text-muted-foreground text-caption">{label}</p>
    </aside>
  );
}
