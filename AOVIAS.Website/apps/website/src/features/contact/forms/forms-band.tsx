'use client';

import { AnimatePresence, motion, useReducedMotion } from 'framer-motion';
import { useCallback, useEffect, useMemo, useState } from 'react';

import {
  ContactForm,
  DemoRequestForm,
  EnterpriseInquiryForm,
  PartnerForm,
} from '@/components/forms';
import { SectionHeader } from '@/components/marketing';
import { Container, Stack, Text } from '@/components/primitives';
import {
  createContactSubmitHandler,
  createDemoRequestSubmitHandler,
  createEnterpriseInquirySubmitHandler,
  createPartnerInquirySubmitHandler,
} from '@/adapters/contact/form-adapters';
import { EngagementInfoPanel } from '@/features/contact/forms/engagement-info-panel';
import { formsContent, formsMeta } from '@/features/contact/forms/content';
import { motionTransitions } from '@/lib/motion/transitions';
import { cn } from '@/lib/utils';

type FormIntent = keyof typeof formsContent;

const INTENT_ORDER: readonly FormIntent[] = [
  'contact',
  'demo',
  'enterprise',
  'partner',
] as const;

function intentFromHash(hash: string): FormIntent | null {
  const id = hash.replace(/^#/, '');
  const match = INTENT_ORDER.find((key) => formsContent[key].id === id);
  return match ?? null;
}

/**
 * Engagement hub — intent cards + form / information two-column layout.
 * Deep links: #form-contact, #form-demo, #form-enterprise, #form-partner
 */
export function ContactFormsBand() {
  const [intent, setIntent] = useState<FormIntent>('contact');
  const reduceMotion = useReducedMotion();

  const selectIntent = useCallback((next: FormIntent, syncHash = true) => {
    setIntent(next);
    if (!syncHash || typeof window === 'undefined') return;
    const nextHash = `#${formsContent[next].id}`;
    if (window.location.hash !== nextHash) {
      window.history.replaceState(null, '', nextHash);
    }
  }, []);

  useEffect(() => {
    const applyHash = () => {
      const fromHash = intentFromHash(window.location.hash);
      if (fromHash) setIntent(fromHash);
    };
    applyHash();
    window.addEventListener('hashchange', applyHash);
    return () => window.removeEventListener('hashchange', applyHash);
  }, []);

  const active = formsContent[intent];

  const formNode = useMemo(() => {
    switch (intent) {
      case 'contact':
        return <ContactForm onSubmit={createContactSubmitHandler()} />;
      case 'demo':
        return <DemoRequestForm onSubmit={createDemoRequestSubmitHandler()} />;
      case 'enterprise':
        return (
          <EnterpriseInquiryForm
            onSubmit={createEnterpriseInquirySubmitHandler()}
          />
        );
      case 'partner':
        return <PartnerForm onSubmit={createPartnerInquirySubmitHandler()} />;
      default: {
        const _exhaustive: never = intent;
        return _exhaustive;
      }
    }
  }, [intent]);

  return (
    <section
      id={formsMeta.sectionId}
      className="bg-muted/40 w-full scroll-mt-24 py-12 sm:py-16 lg:py-20"
      aria-label={formsMeta.title}
    >
      <Container width="wide">
        <Stack gap={8}>
          <SectionHeader
            eyebrow="Engage with AOVIAS"
            heading="Choose how you want to start"
            description="Select an engagement path. The matching form opens below with clear next steps."
          />

          <div
            role="tablist"
            aria-label="Contact intent"
            className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-4"
          >
            {INTENT_ORDER.map((key, index) => {
              const item = formsContent[key];
              const selected = key === intent;
              const Icon = item.icon;
              return (
                <button
                  key={key}
                  type="button"
                  role="tab"
                  id={`tab-${item.id}`}
                  aria-selected={selected}
                  aria-controls={item.id}
                  tabIndex={selected ? 0 : -1}
                  className={cn(
                    'group focus-visible:ring-ring em-interactive flex min-h-[5.5rem] flex-col items-start gap-2 rounded-xl border p-4 text-left transition-[border-color,background-color,box-shadow,transform] duration-[var(--duration-hover)] ease-[var(--easing-standard)] focus-visible:ring-2 focus-visible:outline-none sm:min-h-[6.25rem]',
                    selected
                      ? 'border-primary bg-primary/10 shadow-surface text-foreground'
                      : 'border-border bg-card text-foreground hover:border-primary/40 hover:bg-muted/60',
                  )}
                  onClick={() => selectIntent(key)}
                  onKeyDown={(event) => {
                    if (
                      event.key !== 'ArrowRight' &&
                      event.key !== 'ArrowLeft' &&
                      event.key !== 'ArrowDown' &&
                      event.key !== 'ArrowUp'
                    ) {
                      return;
                    }
                    event.preventDefault();
                    const delta =
                      event.key === 'ArrowRight' || event.key === 'ArrowDown'
                        ? 1
                        : -1;
                    const next =
                      INTENT_ORDER[
                        (index + delta + INTENT_ORDER.length) %
                          INTENT_ORDER.length
                      ];
                    if (next) {
                      selectIntent(next);
                      document.getElementById(`tab-${formsContent[next].id}`)?.focus();
                    }
                  }}
                >
                  <span
                    className={cn(
                      'inline-flex size-8 items-center justify-center rounded-md border',
                      selected
                        ? 'border-primary/40 bg-primary/15 text-primary'
                        : 'border-border bg-muted text-muted-foreground group-hover:text-foreground',
                    )}
                  >
                    <Icon className="size-4" aria-hidden="true" />
                  </span>
                  <span className="text-small font-semibold tracking-tight">
                    {item.heading}
                  </span>
                  <Text as="span" size="caption" tone="muted" className="leading-snug">
                    {item.cardDescription}
                  </Text>
                </button>
              );
            })}
          </div>

          <div className="grid items-start gap-6 lg:grid-cols-[minmax(0,1.65fr)_minmax(0,1fr)] lg:gap-8 xl:gap-10">
            <div
              role="tabpanel"
              id={active.id}
              aria-labelledby={`tab-${active.id}`}
              className="border-border bg-card shadow-surface scroll-mt-24 rounded-xl border p-4 sm:p-6"
            >
              <AnimatePresence mode="wait" initial={false}>
                <motion.div
                  key={intent}
                  initial={reduceMotion ? false : { opacity: 0, y: 8 }}
                  animate={{ opacity: 1, y: 0 }}
                  {...(reduceMotion
                    ? {}
                    : { exit: { opacity: 0, y: -6 } })}
                  transition={motionTransitions.enter}
                >
                  <Stack gap={4}>
                    <div>
                      <Text
                        as="p"
                        size="caption"
                        weight="medium"
                        tone="muted"
                        className="mb-1 tracking-wide uppercase"
                      >
                        {active.eyebrow}
                      </Text>
                      <Text
                        as="p"
                        size="body-lg"
                        weight="semibold"
                        className="mb-1"
                      >
                        {active.heading}
                      </Text>
                      <Text as="p" size="small" tone="muted">
                        {active.description}
                      </Text>
                    </div>
                    {formNode}
                  </Stack>
                </motion.div>
              </AnimatePresence>
            </div>

            <EngagementInfoPanel className="lg:sticky lg:top-24" />
          </div>
        </Stack>
      </Container>
    </section>
  );
}
