'use client';

import {
  ContactForm,
  DemoRequestForm,
  EnterpriseInquiryForm,
  NewsletterForm,
  PartnerForm,
} from '@/components/forms';
import {
  Alert,
  Heading,
  Paragraph,
  Stack,
  Text,
} from '@/components/primitives';
import {
  createMockSubmitHandler,
  FormErrorBanner,
  FormSuccess,
} from '@/features/forms';

function ShowcaseBlock({
  id,
  title,
  notes,
  children,
}: {
  id: string;
  title: string;
  notes?: string;
  children: React.ReactNode;
}) {
  return (
    <section id={id} className="border-border scroll-mt-20 border-b py-12">
      <Stack gap={6}>
        <Stack gap={2}>
          <Heading level={2} size="h2">
            {title}
          </Heading>
          {notes ? (
            <Paragraph tone="muted">{notes}</Paragraph>
          ) : null}
        </Stack>
        <div className="max-w-3xl">{children}</div>
      </Stack>
    </section>
  );
}

const nav = [
  { href: '#contact', label: 'Contact' },
  { href: '#demo', label: 'Demo request' },
  { href: '#newsletter', label: 'Newsletter' },
  { href: '#enterprise', label: 'Enterprise inquiry' },
  { href: '#partner', label: 'Partner' },
  { href: '#states', label: 'Success / error' },
] as const;

/**
 * Internal gallery for Form Engine configs and states.
 */
export function FormsShowcase() {
  return (
    <div>
      <nav aria-label="Form components" className="border-border mb-8 border-b pb-6">
        <ul className="flex flex-wrap gap-x-4 gap-y-2">
          {nav.map((item) => (
            <li key={item.href}>
              <a
                href={item.href}
                className="text-muted-foreground hover:text-foreground text-small underline-offset-4 hover:underline"
              >
                {item.label}
              </a>
            </li>
          ))}
        </ul>
      </nav>

      <Alert tone="info" title="No backend in Sprint 2.4" className="mb-8">
        Submissions use a local mock adapter. Wire CRM / Identity adapters later
        without changing form UIs.
      </Alert>

      <ShowcaseBlock
        id="contact"
        title="Contact Form"
        notes="Dynamic sections + consent. Validated via contactFormConfig."
      >
        <ContactForm onSubmit={createMockSubmitHandler()} />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="demo"
        title="Demo Request Form"
        notes="Includes cross-field validation: phone required when preferred contact is phone."
      >
        <DemoRequestForm onSubmit={createMockSubmitHandler()} />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="newsletter"
        title="Newsletter Form"
        notes="Email, preference switches/checkboxes, and consent."
      >
        <NewsletterForm onSubmit={createMockSubmitHandler()} />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="enterprise"
        title="Enterprise Inquiry Form"
        notes="Organization, requirements, and project summary."
      >
        <EnterpriseInquiryForm onSubmit={createMockSubmitHandler()} />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="partner"
        title="Partner Form"
        notes="Partnership type, contact details, and hidden source field."
      >
        <PartnerForm onSubmit={createMockSubmitHandler()} />
      </ShowcaseBlock>

      <ShowcaseBlock
        id="states"
        title="Success & Error States"
        notes="Shared presentation components used by FormEngine."
      >
        <Stack gap={6}>
          <Stack gap={2}>
            <Text as="p" size="small" weight="medium">
              Success
            </Text>
            <FormSuccess
              title="Request received"
              message="Thank you. Your submission was successful."
              followUp="A specialist will follow up during business hours."
            />
          </Stack>
          <Stack gap={2}>
            <Text as="p" size="small" weight="medium">
              Submission error (retryable)
            </Text>
            <FormErrorBanner
              message="Submission could not be completed. Please try again."
              onRetry={() => undefined}
            />
          </Stack>
          <Stack gap={2}>
            <Text as="p" size="small" weight="medium">
              Forced failure path (Demo form mock)
            </Text>
            <DemoRequestForm
              onSubmit={createMockSubmitHandler({
                fail: true,
                delayMs: 400,
              })}
            />
          </Stack>
        </Stack>
      </ShowcaseBlock>
    </div>
  );
}
