import { cn } from '@/lib/utils';

export type PlatformOrchestrationProps = {
  title?: string;
  className?: string;
};

/**
 * Homepage signature visual — Command + Studio → Platform → industries.
 * Enterprise glow diagram (hero-home); self-contained dark frame.
 */
export function PlatformOrchestration({
  title = 'AOVIAS platform orchestration',
  className,
}: PlatformOrchestrationProps) {
  return (
    <div
      role="img"
      aria-label={title}
      className={cn(
        'relative flex size-full flex-col overflow-hidden rounded-2xl',
        'shadow-[0_0_24px_rgba(74,114,173,0.28)]',
        'aspect-[4/3] min-h-[16rem] xl:min-h-[22rem]',
        className,
      )}
    >
      <svg
        viewBox="0 0 560 420"
        className="size-full flex-1"
        aria-hidden
      >
        <defs>
          <linearGradient id="po-bgGrad" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#111a2c" />
            <stop offset="1" stopColor="#0a0e17" />
          </linearGradient>
          <filter
            id="po-glow"
            x="-40%"
            y="-40%"
            width="180%"
            height="180%"
          >
            <feGaussianBlur stdDeviation="2.4" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
          <filter
            id="po-glow-strong"
            x="-50%"
            y="-50%"
            width="200%"
            height="200%"
          >
            <feGaussianBlur stdDeviation="4" result="blur" />
            <feMerge>
              <feMergeNode in="blur" />
              <feMergeNode in="SourceGraphic" />
            </feMerge>
          </filter>
          <radialGradient id="po-radar" cx="50%" cy="48%" r="55%">
            <stop offset="0%" stopColor="#3b82f6" stopOpacity="0.1" />
            <stop offset="55%" stopColor="#1d4ed8" stopOpacity="0.04" />
            <stop offset="100%" stopColor="#0a0e17" stopOpacity="0" />
          </radialGradient>
          <linearGradient id="po-panel" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor="#132033" />
            <stop offset="100%" stopColor="#0c1626" />
          </linearGradient>
          <linearGradient id="po-hub" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor="#163056" />
            <stop offset="100%" stopColor="#0e1c33" />
          </linearGradient>
          <clipPath id="po-cardClip">
            <rect x="0" y="0" width="560" height="420" rx="24" />
          </clipPath>
        </defs>

        <g clipPath="url(#po-cardClip)">
        {/* Card fill — same as Why AOVIAS exists */}
        <rect width="560" height="420" fill="url(#po-bgGrad)" />
        <rect width="560" height="420" fill="url(#po-radar)" />
        {[70, 120, 170, 220].map((r) => (
          <circle
            key={r}
            cx="280"
            cy="200"
            r={r}
            fill="none"
            stroke="#3b82f6"
            strokeOpacity={0.07}
            strokeWidth="1"
          />
        ))}

        {/* Neon connectors — top products → hub */}
        <g
          fill="none"
          stroke="#60a5fa"
          strokeWidth="1.75"
          strokeLinecap="round"
          filter="url(#po-glow)"
        >
          <path d="M140 92 C140 128, 210 138, 240 168" opacity="0.9" />
          <path d="M420 92 C420 128, 350 138, 320 168" opacity="0.9" />
          {/* Hub → industries */}
          <path d="M230 252 C200 278, 120 290, 88 318" opacity="0.85" />
          <path d="M250 256 C235 290, 200 300, 196 318" opacity="0.85" />
          <path d="M310 256 C325 290, 360 300, 364 318" opacity="0.85" />
          <path d="M330 252 C360 278, 440 290, 472 318" opacity="0.85" />
        </g>

        {/* AOVIAS Command */}
        <g>
          <rect
            x="48"
            y="28"
            width="184"
            height="72"
            rx="14"
            fill="url(#po-panel)"
            stroke="#3b82f6"
            strokeOpacity="0.55"
            strokeWidth="1.25"
            filter="url(#po-glow)"
          />
          <g transform="translate(66 48)" fill="none" stroke="#60a5fa" strokeWidth="1.5">
            <circle cx="12" cy="12" r="10" />
            <circle cx="12" cy="12" r="3.5" fill="#60a5fa" stroke="none" />
            <path d="M12 2 V6 M12 18 V22 M2 12 H6 M18 12 H22" strokeLinecap="round" />
          </g>
          <text x="98" y="54" fill="#f8fafc" fontSize="13" fontWeight="700" fontFamily="system-ui,sans-serif">
            AOVIAS Command
          </text>
          <text x="98" y="72" fill="#93c5fd" fontSize="9" fontFamily="system-ui,sans-serif">
            Operational Command Center
          </text>
          <text x="66" y="88" fill="#94a3b8" fontSize="8" fontFamily="system-ui,sans-serif">
            Governance · Execution · Intelligence
          </text>
        </g>

        {/* AOVIAS Studio */}
        <g>
          <rect
            x="328"
            y="28"
            width="184"
            height="72"
            rx="14"
            fill="url(#po-panel)"
            stroke="#3b82f6"
            strokeOpacity="0.55"
            strokeWidth="1.25"
            filter="url(#po-glow)"
          />
          <g transform="translate(346 50)" fill="none" stroke="#60a5fa" strokeWidth="1.5">
            <rect x="1" y="1" width="18" height="18" rx="3" />
            <path d="M7 15 L7 11 L15 5 L18 8 L10 16 Z" />
          </g>
          <text x="378" y="52" fill="#f8fafc" fontSize="13" fontWeight="700" fontFamily="system-ui,sans-serif">
            AOVIAS Studio
          </text>
          <text x="378" y="66" fill="#93c5fd" fontSize="8.5" fontFamily="system-ui,sans-serif">
            <tspan x="378" dy="0">
              Authoring &amp; Governance
            </tspan>
            <tspan x="378" dy="11">
              Environment
            </tspan>
          </text>
          <text x="346" y="90" fill="#94a3b8" fontSize="8" fontFamily="system-ui,sans-serif">
            Model · Knowledge · Standards
          </text>
        </g>

        {/* Platform hub */}
        <g>
          <rect
            x="168"
            y="158"
            width="224"
            height="98"
            rx="16"
            fill="url(#po-hub)"
            stroke="#60a5fa"
            strokeWidth="1.75"
            filter="url(#po-glow-strong)"
          />
          {/* Brand mark — geometric A (matches BrandMark lockup) */}
          <g transform="translate(188 186)">
            <rect
              x="0"
              y="0"
              width="36"
              height="36"
              rx="8"
              fill="#0B1220"
              fillOpacity="0.35"
              stroke="#60a5fa"
              strokeWidth="1.25"
            />
            <path
              d="M8 28 L18 10 L28 28"
              fill="none"
              stroke="#93c5fd"
              strokeWidth="2"
              strokeLinejoin="round"
            />
            <path
              d="M12.5 22 H23.5"
              fill="none"
              stroke="#60a5fa"
              strokeWidth="2"
              strokeLinecap="round"
              opacity="0.85"
            />
          </g>
          <text x="236" y="198" fill="#f8fafc" fontSize="16" fontWeight="700" fontFamily="system-ui,sans-serif">
            AOVIAS Platform
          </text>
          <text x="236" y="216" fill="#60a5fa" fontSize="11" fontWeight="600" fontFamily="system-ui,sans-serif">
            Shared Foundation
          </text>
          <text x="236" y="234" fill="#94a3b8" fontSize="9" fontFamily="system-ui,sans-serif">
            Identity · Runtime · Deployment
          </text>
          <text x="236" y="248" fill="#64748b" fontSize="8.5" fontFamily="system-ui,sans-serif">
            Infrastructure · Shared Services
          </text>
        </g>

        {/* Industry nodes */}
        <IndustryNode
          x={28}
          y={318}
          label="Healthcare"
          line1="Resilient operations"
          line2="Better outcomes"
          icon="health"
        />
        <IndustryNode
          x={156}
          y={318}
          label="Government"
          line1="Transparent systems"
          line2="Stronger governance"
          icon="gov"
        />
        <IndustryNode
          x={284}
          y={318}
          label="Manufacturing"
          line1="Reliable processes"
          line2="Operational excellence"
          icon="mfg"
        />
        <IndustryNode
          x={412}
          y={318}
          label="Financial"
          line1="Secure operations"
          line2="Trusted services"
          icon="fin"
        />

        {/* Hairline border — same as Why AOVIAS exists */}
        <rect
          x="0.5"
          y="0.5"
          width="559"
          height="419"
          rx="23.5"
          fill="none"
          stroke="#ffffff"
          strokeOpacity="0.08"
        />
        </g>
      </svg>
    </div>
  );
}

type IndustryIcon = 'health' | 'gov' | 'mfg' | 'fin';

function IndustryNode({
  x,
  y,
  label,
  line1,
  line2,
  icon,
}: {
  x: number;
  y: number;
  label: string;
  line1: string;
  line2: string;
  icon: IndustryIcon;
}) {
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={120}
        height={78}
        rx={12}
        fill="url(#po-panel)"
        stroke="#38bdf8"
        strokeOpacity={0.45}
        strokeWidth={1.15}
        filter="url(#po-glow)"
      />
      <g
        transform={`translate(${x + 14} ${y + 14})`}
        fill="none"
        stroke="#7dd3fc"
        strokeWidth={1.4}
      >
        {icon === 'health' ? (
          <>
            <path d="M12 21 C12 21 2 14 2 8.5 C2 5.5 4.5 3 7.5 3 C9.5 3 11.2 4.1 12 5.5 C12.8 4.1 14.5 3 16.5 3 C19.5 3 22 5.5 22 8.5 C22 14 12 21 12 21 Z" />
            <path d="M7 11 H10 L12 8 L14.5 14 L16.5 11 H19" strokeLinecap="round" strokeLinejoin="round" />
          </>
        ) : null}
        {icon === 'gov' ? (
          <>
            <path d="M2 20 H22" strokeLinecap="round" />
            <path d="M4 20 V10 H20 V20" />
            <path d="M2 10 L12 4 L22 10" strokeLinejoin="round" />
            <path d="M8 20 V13 M12 20 V13 M16 20 V13" />
          </>
        ) : null}
        {icon === 'mfg' ? (
          <>
            <path d="M3 20 V10 L9 10 L12 6 H21 V20 Z" strokeLinejoin="round" />
            <path d="M7 20 V15 H10 V20" />
            <path d="M14 11 H18 M14 14 H18" strokeLinecap="round" />
          </>
        ) : null}
        {icon === 'fin' ? (
          <>
            <path d="M2 9 L12 3 L22 9" strokeLinejoin="round" />
            <path d="M4 9 V18 H20 V9" />
            <path d="M2 18 H22" strokeLinecap="round" />
            <path d="M8 18 V13 M12 18 V13 M16 18 V13" />
          </>
        ) : null}
      </g>
      <text
        x={x + 42}
        y={y + 26}
        fill="#f1f5f9"
        fontSize={11}
        fontWeight={700}
        fontFamily="system-ui,sans-serif"
      >
        {label}
      </text>
      <text
        x={x + 14}
        y={y + 50}
        fill="#94a3b8"
        fontSize={8.5}
        fontFamily="system-ui,sans-serif"
      >
        {line1}
      </text>
      <text
        x={x + 14}
        y={y + 64}
        fill="#64748b"
        fontSize={8}
        fontFamily="system-ui,sans-serif"
      >
        {line2}
      </text>
    </g>
  );
}
