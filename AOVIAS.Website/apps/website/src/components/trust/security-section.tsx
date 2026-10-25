import type { ReactNode } from 'react';

import { MarketingCtaLink } from '@/components/marketing';
import { MarketingReveal } from '@/components/marketing/reveal';
import type { MarketingCta } from '@/components/marketing/types';
import type { SecurityControl } from '@/components/trust/models';
import {
  Card,
  CardDescription,
  CardHeader,
  CardTitle,
  Container,
  Grid,
  Heading,
  IconWrapper,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * SecuritySection — security narrative with highlight chips and control cards.
 */
export type SecuritySectionProps = {
  heading: string;
  description?: string;
  /** Short highlight strings (not exaggerated claims — pass measured facts). */
  highlights?: readonly string[];
  controls?: readonly SecurityControl[];
  cta?: MarketingCta;
  className?: string;
  reveal?: boolean;
  footer?: ReactNode;
};

export function SecuritySection({
  heading,
  description,
  highlights,
  controls,
  cta,
  className,
  reveal = true,
  footer,
}: SecuritySectionProps) {
  return (
    <section className={cn('w-full py-section-y-lg', className)}>
      <Container width="wide">
        <MarketingReveal enabled={reveal}>
          <Stack gap={8}>
            <Stack gap={3} className="max-w-2xl">
              <Heading level={2} size="h2">
                {heading}
              </Heading>
              {description ? <Paragraph tone="muted">{description}</Paragraph> : null}
              {highlights && highlights.length > 0 ? (
                <ul className="mt-2 flex flex-col gap-2">
                  {highlights.map((item) => (
                    <li key={item} className="flex items-start gap-2">
                      <span
                        aria-hidden="true"
                        className="bg-primary mt-2 size-1.5 shrink-0 rounded-full"
                      />
                      <Text as="span" size="small">
                        {item}
                      </Text>
                    </li>
                  ))}
                </ul>
              ) : null}
              {cta ? (
                <div className="pt-2">
                  <MarketingCtaLink {...cta} variant="outline" />
                </div>
              ) : null}
            </Stack>

            {controls && controls.length > 0 ? (
              <Grid cols={controls.length === 2 ? 2 : 3} gap={6}>
                {controls.map((control) => (
                  <Card key={control.id} variant="default" className="h-full">
                    <CardHeader>
                      {control.icon ? (
                        <IconWrapper tone="domain" size="lg" className="mb-2">
                          {control.icon}
                        </IconWrapper>
                      ) : null}
                      {control.category ? (
                        <Text
                          as="p"
                          size="caption"
                          tone="muted"
                          weight="medium"
                          className="mb-1 tracking-wide uppercase"
                        >
                          {control.category}
                        </Text>
                      ) : null}
                      <CardTitle>{control.title}</CardTitle>
                      <CardDescription>{control.description}</CardDescription>
                    </CardHeader>
                  </Card>
                ))}
              </Grid>
            ) : null}
            {footer}
          </Stack>
        </MarketingReveal>
      </Container>
    </section>
  );
}
