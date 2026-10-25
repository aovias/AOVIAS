import { Check } from 'lucide-react';

import { OptimizedImage } from '@/components/media';
import {
  HealthcareWorkspaceShot,
  type HealthcareWorkspaceId,
} from '@/features/industries/editorial/healthcare-workspace-shots';
import { ProductAppFrame } from '@/features/industries/editorial/product-app-frame';
import type { IndustryProductShot } from '@/features/industries/editorial/types';
import { cn } from '@/lib/utils';

export type ProductExperienceShowcaseProps = {
  items: readonly IndustryProductShot[];
  className?: string;
};

const WORKSPACE_IDS = new Set<string>([
  'executive',
  'compliance',
  'evidence',
  'capa',
  'dept',
  'mobile',
]);

function isWorkspaceId(id: string): id is HealthcareWorkspaceId {
  return WORKSPACE_IDS.has(id);
}

function isRasterCapture(src: string): boolean {
  return /\.(png|jpe?g|webp|avif)(\?.*)?$/i.test(src);
}

/**
 * Alternating product showcase — large framed screenshots with category,
 * description, and capability bullets. Desktop: zigzag; tablet/mobile: stacked.
 */
export function ProductExperienceShowcase({
  items,
  className,
}: ProductExperienceShowcaseProps) {
  return (
    <div className={cn('flex flex-col gap-16 md:gap-20 xl:gap-24', className)}>
      {items.map((item, index) => (
        <ShowcaseRow key={item.id} item={item} reverse={index % 2 === 1} />
      ))}
    </div>
  );
}

function ShowcaseRow({
  item,
  reverse,
}: {
  item: IndustryProductShot;
  reverse: boolean;
}) {
  const frame = item.frame ?? (item.id === 'mobile' ? 'mobile' : 'desktop');
  const useCapture = isRasterCapture(item.imageSrc);

  return (
    <article
      className={cn(
        'em-product-showcase-row grid items-center gap-8 lg:grid-cols-2 lg:gap-12 xl:gap-16',
      )}
    >
      <div
        className={cn(
          'min-w-0',
          reverse ? 'lg:order-2' : 'lg:order-1',
          frame === 'mobile' && 'flex justify-center lg:justify-start',
          reverse && frame === 'mobile' && 'lg:justify-end',
        )}
      >
        <ProductAppFrame label={item.title} variant={frame}>
          {useCapture ? (
            <OptimizedImage
              src={item.imageSrc}
              alt={item.imageAlt}
              width={frame === 'mobile' ? 720 : 1440}
              height={frame === 'mobile' ? 1280 : 900}
              preset="content"
              className="size-full object-cover object-top"
              useDefaultBlur
            />
          ) : isWorkspaceId(item.id) ? (
            <HealthcareWorkspaceShot id={item.id} />
          ) : (
            // eslint-disable-next-line @next/next/no-img-element -- SVG product surface fallback
            <img
              src={item.imageSrc}
              alt={item.imageAlt}
              className="size-full object-cover object-top"
              loading="lazy"
              decoding="async"
            />
          )}
        </ProductAppFrame>
      </div>

      <div
        className={cn(
          'min-w-0 max-w-xl',
          reverse ? 'lg:order-1' : 'lg:order-2',
          reverse && 'lg:justify-self-end',
        )}
      >
        {item.category ? (
          <p className="text-domain-accent text-caption mb-3 font-semibold tracking-[0.08em] uppercase">
            {item.category}
          </p>
        ) : null}
        <h3 className="text-foreground text-h3 font-semibold tracking-tight">
          {item.title}
        </h3>
        <p className="text-muted-foreground mt-4 text-body leading-relaxed">
          {item.description}
        </p>
        {item.capabilities && item.capabilities.length > 0 ? (
          <ul className="mt-6 flex flex-col gap-3">
            {item.capabilities.map((capability) => (
              <li
                key={capability}
                className="text-foreground flex items-start gap-3 text-small"
              >
                <span className="bg-primary/15 text-domain-accent mt-0.5 flex size-5 shrink-0 items-center justify-center rounded-full">
                  <Check className="size-3 stroke-[2.5]" aria-hidden />
                </span>
                <span className="leading-snug">{capability}</span>
              </li>
            ))}
          </ul>
        ) : null}
      </div>
    </article>
  );
}
