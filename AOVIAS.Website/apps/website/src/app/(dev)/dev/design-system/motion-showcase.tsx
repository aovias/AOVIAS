'use client';

import {
  Badge,
  Caption,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  Grid,
  Heading,
  Paragraph,
  Section,
  Stack,
  Surface,
  Text,
} from '@/components/primitives';
import {
  listMotionRegistry,
  MotionFade,
  MotionHover,
  MotionReveal,
  MotionStagger,
  MotionStaggerItem,
  motionDurationsMs,
  motionTransitions,
} from '@/lib/motion';

export function MotionShowcase() {
  const registry = listMotionRegistry();

  return (
    <Section spacing="lg" className="border-border border-b">
      <Stack gap={8}>
        <Stack gap={2}>
          <Heading level={2} size="h3">
            Motion Foundation
          </Heading>
          <Paragraph tone="muted" size="small">
            Centralized Framer Motion tokens, transitions, registry variants, hover, scroll
            reveal, and reduced-motion support. Toggle OS “Reduce motion” to verify calm
            fallbacks. This section demonstrates the architecture — it does not define page
            marketing motion.
          </Paragraph>
        </Stack>

        <Stack gap={3}>
          <Text weight="medium">Duration tokens (ms)</Text>
          <Stack direction="horizontal" gap={2} wrap>
            {Object.entries(motionDurationsMs).map(([name, ms]) => (
              <Badge key={name} variant="outline">
                {name}: {ms}ms
              </Badge>
            ))}
          </Stack>
          <Caption>
            Transitions: {Object.keys(motionTransitions).join(', ')}
          </Caption>
        </Stack>

        <Stack gap={3}>
          <Text weight="medium">Scroll reveal (once)</Text>
          <Grid cols={3} gap={4}>
            {(['fadeUp', 'slideLeft', 'scaleIn'] as const).map((name) => (
              <MotionReveal key={name} name={name}>
                <Surface padding="md" className="border-border border">
                  <Text size="small" weight="medium">
                    {name}
                  </Text>
                  <Caption>MotionReveal · registry</Caption>
                </Surface>
              </MotionReveal>
            ))}
          </Grid>
        </Stack>

        <Stack gap={3}>
          <Text weight="medium">Stagger list</Text>
          <MotionStagger className="grid gap-3 sm:grid-cols-3">
            {['One', 'Two', 'Three'].map((label) => (
              <MotionStaggerItem key={label}>
                <Surface tone="muted" padding="sm">
                  <Text size="small">Stagger item {label}</Text>
                </Surface>
              </MotionStaggerItem>
            ))}
          </MotionStagger>
        </Stack>

        <Stack gap={3}>
          <Text weight="medium">Hover system</Text>
          <Stack direction="horizontal" gap={4} wrap>
            <MotionHover name="card">
              <Card variant="elevated" className="w-56">
                <CardHeader>
                  <CardTitle>cardHover</CardTitle>
                  <CardDescription>Subtle lift</CardDescription>
                </CardHeader>
                <CardContent>
                  <Caption>getHoverMotion(&quot;card&quot;)</Caption>
                </CardContent>
              </Card>
            </MotionHover>
            <MotionHover name="button">
              <Surface
                padding="sm"
                className="border-border bg-primary text-primary-foreground border"
              >
                <Text size="small">buttonHover</Text>
              </Surface>
            </MotionHover>
            <MotionFade preset="component">
              <Surface padding="sm" className="border-border border">
                <Text size="small">MotionFade · component</Text>
              </Surface>
            </MotionFade>
          </Stack>
        </Stack>

        <Stack gap={3}>
          <Text weight="medium">Motion Registry</Text>
          <Caption>
            Stable names — import via motionRegistry / getRegistryVariants. Do not invent
            inline animation objects in features.
          </Caption>
          <div className="border-border overflow-x-auto rounded-md border">
            <table className="text-small w-full text-left">
              <thead className="bg-muted text-muted-foreground">
                <tr>
                  <th className="px-3 py-2 font-medium">Name</th>
                  <th className="px-3 py-2 font-medium">Kind</th>
                  <th className="px-3 py-2 font-medium">Usage</th>
                </tr>
              </thead>
              <tbody>
                {registry.map((entry) => (
                  <tr key={entry.name} className="border-border border-t">
                    <td className="text-foreground px-3 py-2 font-mono text-caption">
                      {entry.name}
                    </td>
                    <td className="px-3 py-2">
                      <Badge variant="secondary">{entry.kind}</Badge>
                    </td>
                    <td className="text-muted-foreground px-3 py-2">{entry.usage}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Stack>
      </Stack>
    </Section>
  );
}
