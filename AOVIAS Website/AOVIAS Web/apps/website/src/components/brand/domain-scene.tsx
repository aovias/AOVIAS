import { cn } from '@/lib/utils';

import {
  BrandConnector,
  BrandGrid,
  BrandLabel,
  BrandPanel,
  BrandShell,
  brandInk,
} from './brand-primitives';

export type DomainSceneVariant =
  | 'healthcare'
  | 'government'
  | 'education'
  | 'enterprise'
  | 'manufacturing'
  | 'financial'
  | 'security'
  | 'trust'
  | 'operations'
  | 'architecture'
  | 'ai'
  | 'deployment'
  | 'monitoring'
  | 'industry'
  | 'mission'
  | 'contact'
  | 'careers'
  | 'ecosystem';

export type DomainSceneProps = {
  title?: string;
  caption?: string;
  className?: string;
  variant?: DomainSceneVariant;
};

/**
 * Domain / institutional scene — one family, variant structure for sector storytelling.
 */
export function DomainScene({
  title = 'Institutional platform',
  caption,
  className,
  variant = 'mission',
}: DomainSceneProps) {
  return (
    <BrandShell
      title={title}
      {...(caption ? { caption } : {})}
      className={cn('aspect-[4/3]', className)}
      aspectClassName="min-h-[12rem] aspect-[4/3]"
    >
      <svg viewBox="0 0 400 300" className="text-primary absolute inset-0 size-full" aria-hidden>
        <BrandGrid width={400} height={300} step={24} opacity={0.05} />
        <DomainDrawing variant={variant} />
      </svg>
    </BrandShell>
  );
}

function DomainDrawing({ variant }: { variant: DomainSceneVariant }) {
  switch (variant) {
    case 'healthcare':
      return (
        <>
          <BrandPanel x={40} y={40} width={200} height={48} opacity={brandInk.fillMid} stroked />
          <BrandLabel x={140} y={68} size={10}>
            Care continuity
          </BrandLabel>
          <BrandPanel x={40} y={110} width={90} height={140} opacity={brandInk.fillSoft} />
          <BrandPanel x={145} y={110} width={90} height={140} opacity={brandInk.fillMid} />
          <BrandPanel x={250} y={110} width={110} height={140} opacity={brandInk.fillSoft} />
          <BrandConnector d="M85 110 V90 H280 V110" opacity={0.25} />
          <BrandLabel x={85} y={185} size={9}>
            Ward
          </BrandLabel>
          <BrandLabel x={190} y={185} size={9}>
            Ops
          </BrandLabel>
          <BrandLabel x={305} y={185} size={9}>
            Compliance
          </BrandLabel>
        </>
      );
    case 'government':
      return (
        <>
          <BrandPanel x={80} y={36} width={240} height={40} opacity={brandInk.fillStrong} stroked />
          <BrandLabel x={200} y={60} size={10}>
            Public accountability
          </BrandLabel>
          <BrandPanel x={48} y={100} width={90} height={150} opacity={brandInk.fillSoft} />
          <BrandPanel x={155} y={100} width={90} height={150} opacity={brandInk.fillMid} />
          <BrandPanel x={262} y={100} width={90} height={150} opacity={brandInk.fillSoft} />
          <BrandConnector d="M93 100 V76 H200 V76 H307 V100" opacity={0.25} />
        </>
      );
    case 'education':
      return (
        <>
          <BrandPanel x={36} y={50} width={160} height={200} opacity={brandInk.fillSoft} stroked />
          <BrandPanel x={210} y={50} width={154} height={90} opacity={brandInk.fillMid} />
          <BrandPanel x={210} y={160} width={154} height={90} opacity={brandInk.fillMid} />
          <BrandLabel x={116} y={140} size={10}>
            Learning
          </BrandLabel>
          <BrandLabel x={287} y={100} size={9}>
            Curriculum
          </BrandLabel>
          <BrandLabel x={287} y={210} size={9}>
            Outcomes
          </BrandLabel>
          <BrandConnector d="M196 100 H210 M196 200 H210" opacity={0.28} />
        </>
      );
    case 'enterprise':
      return (
        <>
          <BrandPanel x={40} y={48} width={320} height={36} opacity={brandInk.fillMid} stroked />
          <BrandLabel x={200} y={70} size={10}>
            Operating model
          </BrandLabel>
          <BrandPanel x={40} y={108} width={100} height={140} opacity={brandInk.fillSoft} />
          <BrandPanel x={150} y={108} width={100} height={140} opacity={brandInk.fillMid} />
          <BrandPanel x={260} y={108} width={100} height={140} opacity={brandInk.fillSoft} />
          <BrandLabel x={90} y={180} size={9}>
            Teams
          </BrandLabel>
          <BrandLabel x={200} y={180} size={9}>
            Systems
          </BrandLabel>
          <BrandLabel x={310} y={180} size={9}>
            Governance
          </BrandLabel>
        </>
      );
    case 'manufacturing':
      return (
        <>
          <BrandPanel x={36} y={40} width={328} height={40} opacity={brandInk.fillMid} stroked />
          <BrandLabel x={200} y={64} size={10}>
            Production continuity
          </BrandLabel>
          <BrandPanel x={36} y={100} width={70} height={150} opacity={brandInk.fillSoft} />
          <BrandPanel x={118} y={100} width={70} height={150} opacity={brandInk.fillMid} />
          <BrandPanel x={200} y={100} width={70} height={150} opacity={brandInk.fillStrong} />
          <BrandPanel x={282} y={100} width={82} height={150} opacity={brandInk.fillSoft} />
          <BrandLabel x={71} y={180} size={8}>
            Plan
          </BrandLabel>
          <BrandLabel x={153} y={180} size={8}>
            Produce
          </BrandLabel>
          <BrandLabel x={235} y={180} size={8}>
            Quality
          </BrandLabel>
          <BrandLabel x={323} y={180} size={8}>
            Maintain
          </BrandLabel>
          <BrandConnector d="M106 175 H118 M188 175 H200 M270 175 H282" opacity={0.3} />
        </>
      );
    case 'financial':
      return (
        <>
          <BrandPanel x={70} y={36} width={260} height={44} opacity={brandInk.fillStrong} stroked />
          <BrandLabel x={200} y={62} size={10}>
            Institutional oversight
          </BrandLabel>
          <BrandPanel x={48} y={100} width={140} height={70} opacity={brandInk.fillMid} stroked />
          <BrandPanel x={212} y={100} width={140} height={70} opacity={brandInk.fillMid} stroked />
          <BrandPanel x={48} y={190} width={140} height={70} opacity={brandInk.fillSoft} />
          <BrandPanel x={212} y={190} width={140} height={70} opacity={brandInk.fillSoft} />
          <BrandLabel x={118} y={138} size={9}>
            Risk
          </BrandLabel>
          <BrandLabel x={282} y={138} size={9}>
            Controls
          </BrandLabel>
          <BrandLabel x={118} y={228} size={9}>
            Operations
          </BrandLabel>
          <BrandLabel x={282} y={228} size={9}>
            Audit
          </BrandLabel>
          <BrandConnector d="M118 170 V190 M282 170 V190 M188 135 H212 M188 225 H212" opacity={0.28} />
        </>
      );
    case 'security':
      return (
        <>
          <BrandPanel x={120} y={40} width={160} height={200} opacity={brandInk.fillSoft} stroked />
          <BrandPanel x={145} y={70} width={110} height={140} opacity={brandInk.fillMid} />
          <BrandLabel x={200} y={150} size={10}>
            Controls
          </BrandLabel>
          <BrandConnector d="M80 140 H120 M280 140 H320" opacity={0.3} />
          <BrandPanel x={40} y={120} width={40} height={40} opacity={brandInk.fillAccent} />
          <BrandPanel x={320} y={120} width={40} height={40} opacity={brandInk.fillAccent} />
        </>
      );
    case 'trust':
      return (
        <>
          <BrandPanel x={60} y={60} width={280} height={48} opacity={brandInk.fillMid} stroked />
          <BrandLabel x={200} y={88} size={10}>
            Measured claims
          </BrandLabel>
          <BrandPanel x={60} y={140} width={80} height={80} opacity={brandInk.fillSoft} />
          <BrandPanel x={160} y={140} width={80} height={80} opacity={brandInk.fillSoft} />
          <BrandPanel x={260} y={140} width={80} height={80} opacity={brandInk.fillSoft} />
        </>
      );
    case 'operations':
      return (
        <>
          <BrandPanel x={36} y={80} width={70} height={56} opacity={brandInk.fillMid} />
          <BrandPanel x={122} y={80} width={70} height={56} opacity={brandInk.fillMid} />
          <BrandPanel x={208} y={80} width={70} height={56} opacity={brandInk.fillMid} />
          <BrandPanel x={294} y={80} width={70} height={56} opacity={brandInk.fillMid} />
          <BrandConnector d="M106 108 H122 M192 108 H208 M278 108 H294" opacity={0.35} width={brandInk.stroke} />
          <BrandLabel x={200} y={200} size={10}>
            Observe → Decide → Act → Review
          </BrandLabel>
        </>
      );
    case 'architecture':
      return (
        <>
          <BrandPanel x={40} y={40} width={320} height={44} opacity={brandInk.fillSoft} stroked />
          <BrandPanel x={40} y={104} width={150} height={56} opacity={brandInk.fillMid} />
          <BrandPanel x={210} y={104} width={150} height={56} opacity={brandInk.fillMid} />
          <BrandPanel x={40} y={184} width={320} height={56} opacity={brandInk.fillStrong} stroked />
          <BrandLabel x={200} y={66} size={9}>
            Experience
          </BrandLabel>
          <BrandLabel x={115} y={136} size={9}>
            Control
          </BrandLabel>
          <BrandLabel x={285} y={136} size={9}>
            Delivery
          </BrandLabel>
          <BrandLabel x={200} y={216} size={10}>
            Foundation
          </BrandLabel>
          <BrandConnector d="M115 160 V184 M285 160 V184 M200 84 V104" opacity={0.28} />
        </>
      );
    case 'ai':
      return (
        <>
          <BrandPanel x={140} y={100} width={120} height={80} opacity={brandInk.fillMid} stroked />
          <BrandLabel x={200} y={144} size={10}>
            Oversight
          </BrandLabel>
          <BrandPanel x={40} y={50} width={70} height={40} opacity={brandInk.fillSoft} />
          <BrandPanel x={290} y={50} width={70} height={40} opacity={brandInk.fillSoft} />
          <BrandPanel x={40} y={210} width={70} height={40} opacity={brandInk.fillSoft} />
          <BrandPanel x={290} y={210} width={70} height={40} opacity={brandInk.fillSoft} />
          <BrandConnector d="M110 70 H140 M280 70 H260 M110 230 H140 M280 230 H260 M200 100 V50 M200 180 V230" opacity={0.25} />
        </>
      );
    case 'deployment':
      return (
        <>
          <BrandPanel x={48} y={60} width={100} height={180} opacity={brandInk.fillSoft} stroked />
          <BrandPanel x={168} y={60} width={100} height={180} opacity={brandInk.fillMid} stroked />
          <BrandPanel x={288} y={60} width={80} height={180} opacity={brandInk.fillSoft} stroked />
          <BrandLabel x={98} y={150} size={9}>
            Build
          </BrandLabel>
          <BrandLabel x={218} y={150} size={9}>
            Stage
          </BrandLabel>
          <BrandLabel x={328} y={150} size={9}>
            Live
          </BrandLabel>
          <BrandConnector d="M148 150 H168 M268 150 H288" opacity={0.35} />
        </>
      );
    case 'monitoring':
      return (
        <>
          <BrandPanel x={40} y={48} width={320} height={200} opacity={brandInk.fillFaint} stroked />
          <BrandPanel x={60} y={180} width={24} height={40} opacity={brandInk.fillMid} />
          <BrandPanel x={100} y={140} width={24} height={80} opacity={brandInk.fillMid} />
          <BrandPanel x={140} y={100} width={24} height={120} opacity={brandInk.fillStrong} />
          <BrandPanel x={180} y={120} width={24} height={100} opacity={brandInk.fillMid} />
          <BrandPanel x={220} y={90} width={24} height={130} opacity={brandInk.fillStrong} />
          <BrandPanel x={260} y={150} width={24} height={70} opacity={brandInk.fillMid} />
          <BrandPanel x={300} y={110} width={24} height={110} opacity={brandInk.fillMid} />
          <BrandLabel x={200} y={70} size={10}>
            Operational visibility
          </BrandLabel>
        </>
      );
    case 'ecosystem':
      return (
        <>
          <BrandPanel x={40} y={60} width={140} height={90} opacity={brandInk.fillMid} stroked />
          <BrandPanel x={220} y={60} width={140} height={90} opacity={brandInk.fillMid} stroked />
          <BrandPanel x={100} y={180} width={200} height={70} opacity={brandInk.fillStrong} stroked />
          <BrandConnector d="M110 150 V180 M290 150 V180 M200 150 V180" opacity={0.28} />
          <BrandLabel x={110} y={110} size={9}>
            Command
          </BrandLabel>
          <BrandLabel x={290} y={110} size={9}>
            Studio
          </BrandLabel>
          <BrandLabel x={200} y={218} size={10}>
            Platform
          </BrandLabel>
        </>
      );
    case 'industry':
      return (
        <>
          <BrandPanel x={48} y={48} width={304} height={40} opacity={brandInk.fillMid} stroked />
          <BrandPanel x={48} y={104} width={96} height={120} opacity={brandInk.fillSoft} />
          <BrandPanel x={156} y={104} width={96} height={120} opacity={brandInk.fillMid} />
          <BrandPanel x={264} y={104} width={88} height={120} opacity={brandInk.fillSoft} />
          <BrandLabel x={200} y={72} size={10}>
            Sector experience
          </BrandLabel>
        </>
      );
    case 'contact':
      return (
        <>
          <BrandPanel x={36} y={56} width={180} height={16} opacity={brandInk.fillAccent} />
          <BrandPanel x={36} y={86} width={140} height={10} opacity={brandInk.fillSoft} />
          <BrandPanel x={36} y={120} width={220} height={100} opacity={brandInk.fillSoft} stroked />
          <BrandConnector
            d="M280 150 H360 V210 H280 Z M280 150 L320 175 L360 150"
            opacity={0.35}
            width={brandInk.stroke}
          />
        </>
      );
    case 'careers':
      return (
        <>
          <BrandPanel x={36} y={56} width={180} height={16} opacity={brandInk.fillAccent} />
          <BrandPanel x={36} y={86} width={140} height={10} opacity={brandInk.fillSoft} />
          <BrandPanel x={36} y={120} width={220} height={100} opacity={brandInk.fillSoft} stroked />
          <BrandPanel x={280} y={140} width={80} height={80} opacity={brandInk.fillMid} radius={40} />
        </>
      );
    case 'mission':
    default:
      return (
        <>
          <BrandPanel x={36} y={56} width={180} height={16} opacity={brandInk.fillAccent} />
          <BrandPanel x={36} y={86} width={140} height={10} opacity={brandInk.fillSoft} />
          <BrandPanel x={36} y={120} width={220} height={100} opacity={brandInk.fillSoft} stroked />
          <BrandPanel x={52} y={140} width={80} height={8} opacity={brandInk.fillAccent} radius={2} />
          <BrandPanel x={52} y={160} width={160} height={6} opacity={brandInk.fillSoft} radius={2} />
          <BrandPanel x={52} y={176} width={120} height={6} opacity={brandInk.fillSoft} radius={2} />
          <circle cx={300} cy={70} r={70} fill="currentColor" opacity={0.07} />
        </>
      );
  }
}
