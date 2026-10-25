import { Check } from 'lucide-react';

import { Text } from '@/components/primitives';
import { companyLegal } from '@/config/company-legal';
import { engagementPanelContent } from '@/features/contact/forms/content';
import { cn } from '@/lib/utils';

export type EngagementInfoPanelProps = {
  className?: string;
};

/**
 * Compact engagement sidebar — contact channels, process, and trust cues.
 */
export function EngagementInfoPanel({ className }: EngagementInfoPanelProps) {
  const content = engagementPanelContent;

  return (
    <aside
      className={cn(
        'border-border bg-card/80 shadow-surface h-fit rounded-xl border p-5 sm:p-6',
        className,
      )}
      aria-label={content.title}
    >
      <Text
        as="p"
        size="caption"
        weight="medium"
        tone="muted"
        className="mb-4 tracking-wide uppercase"
      >
        {content.title}
      </Text>

      <dl className="flex flex-col gap-5">
        <div>
          <dt className="text-muted-foreground mb-1 text-caption">{content.phone.label}</dt>
          <dd>
            <a
              href={`tel:${companyLegal.phoneTel}`}
              className="text-foreground hover:text-primary focus-visible:ring-ring inline-flex min-h-8 items-center text-small font-medium underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:outline-none"
            >
              {companyLegal.phoneDisplay}
            </a>
          </dd>
        </div>

        <div>
          <dt className="text-muted-foreground mb-1 text-caption">{content.email.label}</dt>
          <dd>
            <a
              href={`mailto:${companyLegal.enquiryEmail}`}
              className="text-foreground hover:text-primary focus-visible:ring-ring inline-flex min-h-8 items-center text-small font-medium underline-offset-4 hover:underline focus-visible:ring-2 focus-visible:outline-none"
            >
              {companyLegal.enquiryEmail}
            </a>
          </dd>
        </div>

        <div>
          <dt className="text-muted-foreground mb-1 text-caption">
            {content.response.label}
          </dt>
          <dd className="text-foreground text-small font-medium">
            {content.response.value}
          </dd>
        </div>

        <div>
          <dt className="text-muted-foreground mb-1 text-caption">{content.hours.label}</dt>
          <dd className="text-foreground whitespace-pre-line text-small font-medium">
            {content.hours.value}
          </dd>
        </div>

        <div>
          <dt className="text-muted-foreground mb-2 text-caption">
            {content.nextSteps.label}
          </dt>
          <dd>
            <ol className="m-0 flex list-none flex-col gap-2 p-0">
              {content.nextSteps.steps.map((step, index) => (
                <li
                  key={step}
                  className="text-foreground flex items-start gap-2.5 text-small"
                >
                  <span
                    className="bg-primary/15 text-primary mt-0.5 inline-flex size-5 shrink-0 items-center justify-center rounded-full text-caption font-semibold"
                    aria-hidden="true"
                  >
                    {index + 1}
                  </span>
                  <span>{step}</span>
                </li>
              ))}
            </ol>
          </dd>
        </div>

        <div className="border-border border-t pt-5">
          <dt className="text-muted-foreground mb-2 text-caption">{content.trust.label}</dt>
          <dd>
            <ul className="m-0 flex list-none flex-col gap-2 p-0">
              {content.trust.items.map((item) => (
                <li
                  key={item}
                  className="text-foreground flex items-center gap-2 text-small"
                >
                  <Check
                    className="text-domain-accent size-3.5 shrink-0"
                    aria-hidden="true"
                    strokeWidth={2.5}
                  />
                  <span>{item}</span>
                </li>
              ))}
            </ul>
          </dd>
        </div>
      </dl>
    </aside>
  );
}
