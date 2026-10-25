import { SectionHeader } from '@/components/marketing';
import {
  Container,
  Heading,
  Stack,
  Text,
} from '@/components/primitives';
import { officeContent, officeMeta } from '@/features/contact/office/content';

/**
 * Office information — city-level presence, hours, and channels.
 */
export function OfficeInformationSection() {
  return (
    <section
      id={officeMeta.sectionId}
      className="w-full scroll-mt-24 py-16 lg:py-20"
      aria-label={officeMeta.title}
    >
      <Container width="wide">
        <Stack gap={10}>
          <SectionHeader
            eyebrow={officeContent.eyebrow}
            heading={officeContent.heading}
            description={officeContent.description}
          />

          <div className="grid gap-8 sm:grid-cols-2 lg:grid-cols-3">
            <Stack gap={2}>
              <Heading level={3} size="h4">
                {officeContent.headquarters.title}
              </Heading>
              {officeContent.headquarters.lines.map((line) => (
                <Text as="p" key={line} size="small" tone="muted">
                  {line}
                </Text>
              ))}
            </Stack>

            <Stack gap={2}>
              <Heading level={3} size="h4">
                {officeContent.hours.title}
              </Heading>
              <Text as="p" size="small" tone="muted">
                {officeContent.hours.description}
              </Text>
            </Stack>

            <Stack gap={3}>
              <Heading level={3} size="h4">
                {officeContent.channels.title}
              </Heading>
              <ul className="flex list-none flex-col gap-2 p-0">
                {officeContent.channels.items.map((channel) => (
                  <li key={channel.id}>
                    <Text as="span" size="small" weight="medium">
                      {channel.label}:{' '}
                    </Text>
                    <a
                      href={channel.href}
                      className="text-primary text-small underline underline-offset-4"
                    >
                      {channel.value}
                    </a>
                  </li>
                ))}
              </ul>
            </Stack>
          </div>
        </Stack>
      </Container>
    </section>
  );
}
