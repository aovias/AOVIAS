import Link from 'next/link';

import { BrandAtmosphere } from '@/components/brand';
import { PageContainer } from '@/components/layout/page-container';
import { companyLegal } from '@/config/company-legal';
import { footerColumns, socialLinks } from '@/config/navigation';
import { siteConfig } from '@/config/site';
import { getNavIcon } from '@/lib/navigation/icons';
import { cn } from '@/lib/utils';

export type SiteFooterProps = {
  className?: string;
  compact?: boolean;
};

const copyrightLegalLinks = [
  { id: 'privacy', label: 'Privacy', href: '/legal/privacy' },
  { id: 'terms', label: 'Terms', href: '/legal/terms' },
  { id: 'cookies', label: 'Cookie Policy', href: '/legal/cookies' },
  { id: 'security', label: 'Security', href: '/legal/security' },
] as const;

/** Enterprise footer — AVLS institutional close with corporate identity. */
export function SiteFooter({ className, compact = false }: SiteFooterProps) {
  const year = new Date().getFullYear();
  const registryRows = [
    companyLegal.cin ? { label: 'CIN', value: companyLegal.cin } : null,
    companyLegal.gstin ? { label: 'GSTIN', value: companyLegal.gstin } : null,
    companyLegal.tan ? { label: 'TAN', value: companyLegal.tan } : null,
  ].filter(Boolean) as { label: string; value: string }[];

  return (
    <footer
      className={cn(
        'text-brand-100 relative isolate mt-auto overflow-hidden border-t border-brand-800/60 bg-gradient-to-b from-brand-950 via-brand-900 to-brand-950',
        className,
      )}
    >
      <div
        aria-hidden="true"
        className="via-primary/65 absolute inset-x-0 top-0 z-10 h-px bg-gradient-to-r from-transparent to-transparent"
      />
      <BrandAtmosphere
        variant="corporate"
        density="subtle"
        className="opacity-[0.35]"
      />
      <div
        aria-hidden="true"
        className="bg-primary/15 pointer-events-none absolute -top-24 left-1/2 -z-10 h-64 w-[min(42rem,100vw)] -translate-x-1/2 rounded-full blur-3xl"
      />

      <PageContainer
        width="hero"
        className="relative flex flex-col gap-stack-xl py-section-y-lg"
      >
        {compact ? null : (
          <div className="gap-grid-gap grid sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-[minmax(0,1.45fr)_repeat(6,minmax(0,1fr))]">
            <div className="flex flex-col gap-3 sm:col-span-2 md:col-span-3 lg:col-span-1 lg:pr-8">
              <p className="text-brand-50 text-subheading font-semibold tracking-tight">
                {companyLegal.brandName}
              </p>
              <p className="text-brand-100/90 text-small font-medium tracking-wide">
                {companyLegal.tagline}
              </p>
              <p className="text-brand-200/80 max-w-xs text-small leading-relaxed">
                {companyLegal.missionLine}
              </p>
              <nav aria-label="Social" className="mt-3">
                <ul className="flex flex-wrap gap-2">
                  {socialLinks.map((link) => {
                    const Icon = getNavIcon(link.icon);
                    return (
                      <li key={link.id}>
                        <Link
                          href={link.href}
                          target="_blank"
                          rel="noopener noreferrer"
                          aria-label={link.label}
                          className="border-brand-800/70 text-brand-200 hover:border-primary/50 hover:text-brand-50 hover:bg-brand-800/40 focus-visible:ring-primary/60 inline-flex size-9 items-center justify-center rounded-lg border transition-colors focus-visible:ring-2 focus-visible:outline-none"
                        >
                          {Icon ? (
                            <Icon className="size-4" aria-hidden="true" />
                          ) : (
                            <span className="text-caption">{link.label}</span>
                          )}
                        </Link>
                      </li>
                    );
                  })}
                </ul>
              </nav>
            </div>

            {footerColumns.map((column) => (
              <nav key={column.id} aria-label={column.title} className="min-w-0">
                <p className="text-brand-50 mb-3.5 text-small font-semibold tracking-wide">
                  {column.title}
                </p>
                <ul className="flex flex-col gap-2.5">
                  {column.items
                    .filter((item) => !item.hidden)
                    .map((item) => (
                      <li key={item.id}>
                        <Link
                          href={item.href}
                          target={item.external ? '_blank' : undefined}
                          rel={item.external ? 'noreferrer' : undefined}
                          className="text-brand-200/80 hover:text-brand-50 focus-visible:ring-primary/60 text-small transition-colors focus-visible:ring-2 focus-visible:outline-none"
                        >
                          {item.label}
                        </Link>
                      </li>
                    ))}
                </ul>
              </nav>
            ))}
          </div>
        )}

        {compact ? null : (
          <div className="border-brand-800/60 grid gap-6 border-t pt-8 sm:grid-cols-2 lg:grid-cols-[1.2fr_1fr_1fr]">
            <div>
              <p className="text-brand-50 mb-3 text-small font-semibold tracking-wide">
                Company Information
              </p>
              <p className="text-brand-100 text-small font-medium">
                {companyLegal.legalName}
              </p>
              <div className="text-brand-300/80 mt-2 space-y-0.5 text-caption leading-relaxed">
                <p>Registered Office</p>
                {companyLegal.registeredOffice.map((line) => (
                  <p key={line}>{line}</p>
                ))}
                <p className="pt-1">Registered in {companyLegal.registeredIn}</p>
              </div>
              {registryRows.length > 0 ? (
                <dl className="text-brand-300/75 mt-3 space-y-1 text-caption">
                  {registryRows.map((row) => (
                    <div key={row.label} className="flex flex-wrap gap-x-2">
                      <dt className="text-brand-200/70">{row.label}</dt>
                      <dd>{row.value}</dd>
                    </div>
                  ))}
                </dl>
              ) : null}
            </div>
            <div>
              <p className="text-brand-50 mb-3 text-small font-semibold tracking-wide">
                Contact
              </p>
              <ul className="text-brand-300/80 flex flex-col gap-2 text-caption">
                <li>
                  <span className="text-brand-200/70">Phone · </span>
                  <a
                    href={`tel:${companyLegal.phoneTel}`}
                    className="hover:text-brand-50 focus-visible:ring-primary/60 underline-offset-2 hover:underline focus-visible:ring-2 focus-visible:outline-none"
                  >
                    {companyLegal.phoneDisplay}
                  </a>
                </li>
                <li>
                  <span className="text-brand-200/70">Email · </span>
                  <a
                    href={`mailto:${companyLegal.contactEmail}`}
                    className="hover:text-brand-50 focus-visible:ring-primary/60 underline-offset-2 hover:underline focus-visible:ring-2 focus-visible:outline-none"
                  >
                    {companyLegal.contactEmail}
                  </a>
                </li>
                <li>
                  <span className="text-brand-200/70">Support · </span>
                  <a
                    href={`mailto:${companyLegal.supportEmail}`}
                    className="hover:text-brand-50 focus-visible:ring-primary/60 underline-offset-2 hover:underline focus-visible:ring-2 focus-visible:outline-none"
                  >
                    {companyLegal.supportEmail}
                  </a>
                </li>
                <li>
                  <span className="text-brand-200/70">Website · </span>
                  <a
                    href={companyLegal.website}
                    className="hover:text-brand-50 focus-visible:ring-primary/60 underline-offset-2 hover:underline focus-visible:ring-2 focus-visible:outline-none"
                  >
                    {companyLegal.website.replace(/^https?:\/\//, '')}
                  </a>
                </li>
              </ul>
            </div>
            <div className="sm:col-span-2 lg:col-span-1">
              <p className="text-brand-50 mb-3 text-small font-semibold tracking-wide">
                Partner with AOVIAS
              </p>
              <p className="text-brand-200/70 mb-3 text-caption leading-relaxed">
                Speak with our team about deploying AOVIAS across your institution.
              </p>
              <Link
                href="/company/contact"
                className="border-primary/40 bg-primary/15 text-brand-50 hover:bg-primary/25 hover:border-primary/60 focus-visible:ring-primary/60 inline-flex items-center gap-2 rounded-lg border px-4 py-2 text-small font-medium transition-colors focus-visible:ring-2 focus-visible:outline-none"
              >
                Contact us
                <span aria-hidden="true">→</span>
              </Link>
            </div>
          </div>
        )}

        <div className="border-brand-800/60 flex flex-col gap-4 border-t pt-6 text-caption sm:flex-row sm:items-center sm:justify-between">
          <p className="text-brand-300/70">
            © {year} {companyLegal.legalName}. All Rights Reserved.
          </p>

          {compact ? null : (
            <nav aria-label="Legal">
              <ul className="flex flex-wrap items-center gap-x-4 gap-y-2">
                {copyrightLegalLinks.map((item) => (
                  <li key={item.id}>
                    <Link
                      href={item.href}
                      className="text-brand-300/70 hover:text-brand-50 focus-visible:ring-primary/60 transition-colors focus-visible:ring-2 focus-visible:outline-none"
                    >
                      {item.label}
                    </Link>
                  </li>
                ))}
              </ul>
            </nav>
          )}

          <p className="text-brand-300/60 inline-flex items-center gap-2">
            <span
              className="bg-primary/70 inline-block size-1.5 rounded-full"
              aria-hidden="true"
            />
            {siteConfig.release} · {siteConfig.version}
          </p>
        </div>
      </PageContainer>
    </footer>
  );
}
