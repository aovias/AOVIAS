import type { ReactNode } from 'react';

import { Container } from '@/components/primitives';
import { ThemeToggle } from '@/providers/theme';

/**
 * Internal design-system / QA shell — no marketing navigation chrome.
 */
export default function DevLayout({ children }: { children: ReactNode }) {
  return (
    <div className="bg-background text-foreground min-h-dvh">
      <header className="border-border bg-background/95 sticky top-0 z-20 border-b backdrop-blur">
        <Container className="flex h-14 items-center justify-between gap-4">
          <div className="flex min-w-0 items-center gap-4">
            <p className="text-small font-semibold tracking-tight">AOVIAS · Design System</p>
            <nav aria-label="Design system sections" className="text-small hidden gap-3 sm:flex">
              <a
                href="/dev/design-system"
                className="text-muted-foreground hover:text-foreground underline-offset-4 hover:underline"
              >
                Primitives
              </a>
              <a
                href="/internal/design-system"
                className="text-muted-foreground hover:text-foreground underline-offset-4 hover:underline"
              >
                Marketing
              </a>
              <a
                href="/internal/design-system/product"
                className="text-muted-foreground hover:text-foreground underline-offset-4 hover:underline"
              >
                Product
              </a>
              <a
                href="/internal/design-system/product-landing"
                className="text-muted-foreground hover:text-foreground underline-offset-4 hover:underline"
              >
                Landing
              </a>
              <a
                href="/internal/design-system/trust"
                className="text-muted-foreground hover:text-foreground underline-offset-4 hover:underline"
              >
                Trust
              </a>
              <a
                href="/internal/design-system/forms"
                className="text-muted-foreground hover:text-foreground underline-offset-4 hover:underline"
              >
                Forms
              </a>
              <a
                href="/internal/design-system/pricing"
                className="text-muted-foreground hover:text-foreground underline-offset-4 hover:underline"
              >
                Pricing
              </a>
              <a
                href="/internal/design-system/resources"
                className="text-muted-foreground hover:text-foreground underline-offset-4 hover:underline"
              >
                Resources
              </a>
              <a
                href="/internal/design-system/system-states"
                className="text-muted-foreground hover:text-foreground underline-offset-4 hover:underline"
              >
                States
              </a>
            </nav>
          </div>
          <ThemeToggle />
        </Container>
      </header>
      <main id="main-content" tabIndex={-1}>
        {children}
      </main>
    </div>
  );
}
