import { LogoCloud } from '@/components/marketing';
import type { CustomerReference } from '@/components/trust/models';
import { cn } from '@/lib/utils';

/**
 * CustomerLogoGrid — maps CustomerReference models onto LogoCloud.
 */
export type CustomerLogoGridProps = {
  customers: readonly CustomerReference[];
  size?: 'sm' | 'md' | 'lg';
  className?: string;
  label?: string;
};

export function CustomerLogoGrid({
  customers,
  size = 'md',
  className,
  label = 'Customers and institutions',
}: CustomerLogoGridProps) {
  return (
    <LogoCloud
      className={cn(className)}
      size={size}
      label={label}
      items={customers.map((customer) => ({
        id: customer.id,
        name: customer.name,
        ...(customer.logo ? { logo: customer.logo } : {}),
      }))}
    />
  );
}
