import type { DomainSceneProps, DomainSceneVariant } from './domain-scene';
import { DomainScene } from './domain-scene';

export type InstitutionalSceneProps = {
  title?: string;
  caption?: string;
  className?: string;
  variant?: DomainSceneVariant;
};

/**
 * Calm institutional atmosphere for marketing heroes.
 * Backed by DomainScene so all scenes share one illustration family.
 */
export function InstitutionalScene({
  title = 'Institutional platform',
  caption,
  className,
  variant = 'mission',
}: InstitutionalSceneProps) {
  return (
    <DomainScene
      title={title}
      {...(caption ? { caption } : {})}
      {...(className ? { className } : {})}
      variant={variant}
    />
  );
}

export type { DomainSceneProps, DomainSceneVariant };
