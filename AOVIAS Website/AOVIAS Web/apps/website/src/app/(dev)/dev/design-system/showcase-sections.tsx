'use client';

import { Box, Layers, Search } from 'lucide-react';
import type { ReactNode } from 'react';

import {
  Alert,
  AvatarPlaceholder,
  Badge,
  Button,
  Caption,
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
  Checkbox,
  Code,
  Container,
  Divider,
  EmptyState,
  Grid,
  Heading,
  IconButton,
  IconWrapper,
  Input,
  Label,
  Link,
  Paragraph,
  Quote,
  Radio,
  Section,
  Select,
  Separator,
  Skeleton,
  Spacer,
  Spinner,
  Stack,
  Surface,
  Switch,
  Text,
  Textarea,
} from '@/components/primitives';

import { MotionShowcase } from './motion-showcase';

function ShowcaseBlock({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: ReactNode;
}) {
  return (
    <Section spacing="lg" className="border-border border-b">
      <Stack gap={6}>
        <Stack gap={2}>
          <Heading level={2} size="h3">
            {title}
          </Heading>
          <Paragraph tone="muted" size="small">
            {description}
          </Paragraph>
        </Stack>
        {children}
      </Stack>
    </Section>
  );
}

export function ShowcaseSections() {
  return (
    <>
      <MotionShowcase />

      <ShowcaseBlock
        title="Layout"
        description="Container, Section, Grid, Stack, Divider, and Spacer primitives."
      >
        <Stack gap={6}>
          <Surface tone="muted" padding="sm">
            <Text size="small" tone="muted">
              Container widths (visual: nested borders)
            </Text>
            <Stack gap={3} className="mt-3">
              {(['narrow', 'default', 'wide', 'full'] as const).map((width) => (
                <Container
                  key={width}
                  width={width}
                  className="border-border bg-card rounded-md border py-2"
                >
                  <Text size="caption">width=&quot;{width}&quot;</Text>
                </Container>
              ))}
            </Stack>
          </Surface>

          <Grid cols={3} gap={4}>
            <Surface padding="sm">Grid col</Surface>
            <Surface padding="sm">Grid col</Surface>
            <Surface padding="sm">Grid col</Surface>
          </Grid>

          <Stack direction="horizontal" gap={3}>
            <Badge>Horizontal</Badge>
            <Badge variant="primary">Stack</Badge>
            <Divider orientation="vertical" className="h-6" />
            <Badge variant="outline">Divider</Badge>
          </Stack>
          <Spacer size={2} />
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        title="Typography"
        description="Token-driven heading hierarchy, body copy, labels, links, code, and quotes."
      >
        <Stack gap={4}>
          <Heading level={1} size="display-l">
            Display heading
          </Heading>
          <Heading level={2}>Section heading</Heading>
          <Heading level={3} size="h4">
            Subsection
          </Heading>
          <Paragraph>
            Paragraph body copy with relaxed line height for accessible reading.
          </Paragraph>
          <Text tone="muted" size="small">
            Muted Text primitive
          </Text>
          <Caption>Caption supporting text</Caption>
          <Label htmlFor="demo-input">Form label</Label>
          <Paragraph size="small">
            Inline <Link href="/dev/design-system">Link</Link> and <Code>Code</Code> samples.
          </Paragraph>
          <Quote attribution="AOVIAS Design Language">
            Clarity over ornament. Systems over one-offs.
          </Quote>
          <Code block>{`import { Button } from '@/components/primitives';`}</Code>
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        title="Controls"
        description="Buttons, badges, and form control foundations."
      >
        <Stack gap={6}>
          <Stack gap={3}>
            <Text weight="medium">Button variants</Text>
            <Stack direction="horizontal" gap={3} wrap>
              <Button variant="primary">Primary</Button>
              <Button variant="secondary">Secondary</Button>
              <Button variant="ghost">Ghost</Button>
              <Button variant="outline">Outline</Button>
              <Button variant="destructive">Destructive</Button>
              <Button variant="link">Link</Button>
            </Stack>
            <Stack direction="horizontal" gap={3} wrap>
              <Button size="sm">Small</Button>
              <Button size="md">Medium</Button>
              <Button size="lg">Large</Button>
              <Button loading>Loading</Button>
              <Button disabled>Disabled</Button>
            </Stack>
            <Stack direction="horizontal" gap={2}>
              <IconButton aria-label="Search" variant="ghost">
                <Search />
              </IconButton>
              <IconButton aria-label="Layers" variant="outline">
                <Layers />
              </IconButton>
              <IconButton aria-label="Box" variant="primary">
                <Box />
              </IconButton>
            </Stack>
          </Stack>

          <Stack direction="horizontal" gap={2} wrap>
            <Badge>Default</Badge>
            <Badge variant="primary">Primary</Badge>
            <Badge variant="outline">Outline</Badge>
            <Badge variant="success">Success</Badge>
            <Badge variant="warning">Warning</Badge>
            <Badge variant="error">Error</Badge>
            <Badge variant="info">Info</Badge>
          </Stack>

          <Grid cols={2} gap={4}>
            <Stack gap={2}>
              <Label htmlFor="demo-input">Input</Label>
              <Input id="demo-input" placeholder="Placeholder" />
            </Stack>
            <Stack gap={2}>
              <Label htmlFor="demo-select">Select</Label>
              <Select id="demo-select" defaultValue="a">
                <option value="a">Option A</option>
                <option value="b">Option B</option>
              </Select>
            </Stack>
            <Stack gap={2} className="sm:col-span-2">
              <Label htmlFor="demo-textarea">Textarea</Label>
              <Textarea id="demo-textarea" placeholder="Multi-line…" />
            </Stack>
          </Grid>

          <Stack direction="horizontal" gap={6} wrap>
            <label className="flex items-center gap-2 text-small">
              <Checkbox id="demo-check" defaultChecked />
              Checkbox
            </label>
            <label className="flex items-center gap-2 text-small">
              <Radio name="demo-radio" defaultChecked />
              Radio A
            </label>
            <label className="flex items-center gap-2 text-small">
              <Radio name="demo-radio" />
              Radio B
            </label>
            <label className="flex items-center gap-2 text-small">
              <Switch aria-label="Demo switch" defaultChecked />
              Switch
            </label>
          </Stack>
        </Stack>
      </ShowcaseBlock>

      <ShowcaseBlock
        title="Display"
        description="Card slots, surfaces, icon wrapper, avatar placeholder, separator."
      >
        <Grid cols={2} gap={4}>
          <Card variant="default">
            <CardHeader>
              <CardTitle>Default card</CardTitle>
              <CardDescription>Composable header/description slots.</CardDescription>
            </CardHeader>
            <CardContent>
              <Paragraph size="small">Card content region.</Paragraph>
            </CardContent>
            <CardFooter>
              <Button size="sm">Action</Button>
            </CardFooter>
          </Card>
          <Card variant="elevated">
            <CardHeader>
              <CardTitle>Elevated</CardTitle>
              <CardDescription>Raised shadow token.</CardDescription>
            </CardHeader>
            <CardContent>
              <Stack direction="horizontal" gap={3} align="center">
                <AvatarPlaceholder initials="AV" label="AOVIAS avatar" />
                <IconWrapper tone="primary" size="lg">
                  <Layers />
                </IconWrapper>
                <Separator orientation="vertical" className="h-8" />
                <Text size="small">Icon + avatar</Text>
              </Stack>
            </CardContent>
          </Card>
          <Card variant="flat">
            <CardContent>
              <Text weight="medium">Flat card</Text>
            </CardContent>
          </Card>
          <Card variant="interactive" tabIndex={0}>
            <CardContent>
              <Text weight="medium">Interactive card</Text>
              <Caption>Focus-within ring for keyboard users.</Caption>
            </CardContent>
          </Card>
        </Grid>
      </ShowcaseBlock>

      <ShowcaseBlock
        title="Feedback"
        description="Spinner, skeleton, empty state, and alert foundation."
      >
        <Stack gap={4}>
          <Stack direction="horizontal" gap={4} align="center">
            <Spinner size="sm" />
            <Spinner size="md" />
            <Spinner size="lg" />
            <Skeleton className="h-4 w-40" />
            <Skeleton className="size-10 rounded-full" />
          </Stack>
          <Grid cols={2} gap={4}>
            <Alert tone="info" title="Info">
              Informational alert foundation.
            </Alert>
            <Alert tone="success" title="Success">
              Success alert foundation.
            </Alert>
            <Alert tone="warning" title="Warning">
              Warning alert foundation.
            </Alert>
            <Alert tone="error" title="Error" live="assertive">
              Error alert foundation.
            </Alert>
          </Grid>
          <EmptyState
            title="Nothing here yet"
            description="Generic empty state — pass domain copy from the consumer."
            icon={
              <IconWrapper size="xl" tone="muted">
                <Box />
              </IconWrapper>
            }
            action={<Button variant="outline">Primary action slot</Button>}
          />
        </Stack>
      </ShowcaseBlock>
    </>
  );
}
