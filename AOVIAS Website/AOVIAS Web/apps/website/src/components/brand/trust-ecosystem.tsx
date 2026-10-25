import { useId } from 'react';

import { cn } from '@/lib/utils';

export type TrustEcosystemProps = {
  title?: string;
  /** Retained for API compatibility; caption is rendered inside the SVG. */
  caption?: string;
  className?: string;
};

/**
 * Trust principles — Security · Privacy · Identity · Stewardship
 * curving into a central Trust hub.
 */
export function TrustEcosystem({
  title = 'AOVIAS trust principles illustration',
  className,
}: TrustEcosystemProps) {
  const uid = useId().replace(/:/g, '');
  const ids = {
    bgGrad: `trust-bg-${uid}`,
    boxFill: `trust-box-${uid}`,
    trustFill: `trust-fill-${uid}`,
    topGlow: `trust-top-${uid}`,
    trustGlow: `trust-glow-${uid}`,
    nodeGlow: `trust-node-${uid}`,
    hilite: `trust-hi-${uid}`,
    grid: `trust-grid-${uid}`,
    clip: `trust-clip-${uid}`,
    shadow: `trust-shadow-${uid}`,
  };

  return (
    <div
      role="img"
      aria-label={title}
      className={cn(
        'relative w-full overflow-hidden rounded-2xl',
        'shadow-[0_0_28px_rgba(74,114,173,0.22)]',
        'aspect-[730/550] min-h-[14rem] xl:min-h-[18rem]',
        className,
      )}
    >
      <svg
        viewBox="0 0 730 550"
        xmlns="http://www.w3.org/2000/svg"
        className="size-full"
        role="img"
        aria-hidden
      >
        <title>{title}</title>
        <desc>
          Security, Privacy, Identity and Stewardship boxes curve into a central
          Trust box labelled Platform principle, each principle carrying its own
          icon, on a dark navy card with grid background.
        </desc>
        <defs>
          <linearGradient id={ids.bgGrad} x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#111a2c" />
            <stop offset="1" stopColor="#0a0e17" />
          </linearGradient>
          <linearGradient id={ids.boxFill} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#242e48" />
            <stop offset="1" stopColor="#1a2338" />
          </linearGradient>
          <linearGradient id={ids.trustFill} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#3d4d74" />
            <stop offset="1" stopColor="#28324e" />
          </linearGradient>
          <radialGradient id={ids.topGlow} cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#4a72ad" stopOpacity="0.12" />
            <stop offset="1" stopColor="#4a72ad" stopOpacity="0" />
          </radialGradient>
          <radialGradient id={ids.trustGlow} cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#6f93c2" stopOpacity="0.3" />
            <stop offset="1" stopColor="#6f93c2" stopOpacity="0" />
          </radialGradient>
          <radialGradient id={ids.nodeGlow} cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#a8cbf2" stopOpacity="0.55" />
            <stop offset="1" stopColor="#a8cbf2" stopOpacity="0" />
          </radialGradient>
          <linearGradient id={ids.hilite} x1="0" y1="0" x2="1" y2="0">
            <stop offset="0" stopColor="#ffffff" stopOpacity="0" />
            <stop offset="0.5" stopColor="#ffffff" stopOpacity="0.35" />
            <stop offset="1" stopColor="#ffffff" stopOpacity="0" />
          </linearGradient>
          <pattern
            id={ids.grid}
            width="36"
            height="36"
            patternUnits="userSpaceOnUse"
          >
            <path
              d="M36 0H0V36"
              fill="none"
              stroke="#8fb3e0"
              strokeOpacity="0.05"
              strokeWidth="1"
            />
          </pattern>
          <clipPath id={ids.clip}>
            <rect x="10" y="10" width="710" height="530" rx="20" />
          </clipPath>
          <filter
            id={ids.shadow}
            x="-30%"
            y="-30%"
            width="160%"
            height="200%"
          >
            <feDropShadow
              dx="0"
              dy="6"
              stdDeviation="8"
              floodColor="#000000"
              floodOpacity="0.35"
            />
          </filter>
        </defs>

        <g clipPath={`url(#${ids.clip})`}>
          <rect
            x="10"
            y="10"
            width="710"
            height="530"
            fill={`url(#${ids.bgGrad})`}
          />
          <rect
            x="10"
            y="10"
            width="710"
            height="530"
            fill={`url(#${ids.grid})`}
          />
          <line
            x1="560"
            y1="10"
            x2="720"
            y2="150"
            stroke="#ffffff"
            strokeOpacity="0.035"
            strokeWidth="1"
          />
          <line
            x1="620"
            y1="10"
            x2="720"
            y2="95"
            stroke="#ffffff"
            strokeOpacity="0.03"
            strokeWidth="1"
          />
          <circle cx="365" cy="20" r="230" fill={`url(#${ids.topGlow})`} />
          <circle cx="365" cy="271" r="210" fill={`url(#${ids.trustGlow})`} />

          <path
            d="M255,172 C255,201 233,201 233,230"
            fill="none"
            stroke="#8fb3e0"
            strokeOpacity="0.4"
            strokeWidth="1.4"
          />
          <path
            d="M477,172 C477,201 496,201 496,230"
            fill="none"
            stroke="#8fb3e0"
            strokeOpacity="0.4"
            strokeWidth="1.4"
          />
          <path
            d="M255,370 C255,341 233,341 233,312"
            fill="none"
            stroke="#8fb3e0"
            strokeOpacity="0.4"
            strokeWidth="1.4"
          />
          <path
            d="M477,370 C477,341 496,341 496,312"
            fill="none"
            stroke="#8fb3e0"
            strokeOpacity="0.4"
            strokeWidth="1.4"
          />

          <circle cx="244" cy="201" r="5.5" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="244" cy="201" r="2" fill="#cfe0f5" />
          <circle cx="486.5" cy="201" r="5.5" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="486.5" cy="201" r="2" fill="#cfe0f5" />
          <circle cx="244" cy="341" r="5.5" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="244" cy="341" r="2" fill="#cfe0f5" />
          <circle cx="486.5" cy="341" r="5.5" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="486.5" cy="341" r="2" fill="#cfe0f5" />

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="90"
              y="90"
              width="180"
              height="82"
              rx="14"
              fill={`url(#${ids.boxFill})`}
              stroke="#5b7aa8"
              strokeWidth="1"
              strokeOpacity="0.35"
            />
            <path
              d="M172,108 L180,104 L188,108 L188,116 L180,124 L172,116 Z"
              fill="#7ea3d1"
              fillOpacity="0.18"
              stroke="#a8cbf2"
              strokeWidth="1.3"
            />
            <text
              x="180"
              y="148"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="15"
              fontWeight="700"
              fill="#dbe6f7"
            >
              Security
            </text>
          </g>

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="460"
              y="90"
              width="182"
              height="82"
              rx="14"
              fill={`url(#${ids.boxFill})`}
              stroke="#5b7aa8"
              strokeWidth="1"
              strokeOpacity="0.35"
            />
            <path
              d="M544,112 v-4 a5,5 0 0 1 10,0 v4"
              fill="none"
              stroke="#a8cbf2"
              strokeWidth="1.3"
            />
            <rect
              x="542"
              y="112"
              width="14"
              height="11"
              rx="2"
              fill="#7ea3d1"
              fillOpacity="0.18"
              stroke="#a8cbf2"
              strokeWidth="1.3"
            />
            <circle cx="549" cy="117" r="1.2" fill="#a8cbf2" />
            <text
              x="551"
              y="148"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="15"
              fontWeight="700"
              fill="#dbe6f7"
            >
              Privacy
            </text>
          </g>

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="90"
              y="370"
              width="180"
              height="82"
              rx="14"
              fill={`url(#${ids.boxFill})`}
              stroke="#5b7aa8"
              strokeWidth="1"
              strokeOpacity="0.35"
            />
            <circle cx="180" cy="390" r="4" fill="#a8cbf2" />
            <path
              d="M172,405 a8,8 0 0 1 16,0"
              fill="none"
              stroke="#a8cbf2"
              strokeWidth="1.4"
            />
            <text
              x="180"
              y="428"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="15"
              fontWeight="700"
              fill="#dbe6f7"
            >
              Identity
            </text>
          </g>

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="460"
              y="370"
              width="182"
              height="82"
              rx="14"
              fill={`url(#${ids.boxFill})`}
              stroke="#5b7aa8"
              strokeWidth="1"
              strokeOpacity="0.35"
            />
            <path
              d="M543,404 Q551,384 559,404 Q551,412 543,404 Z"
              fill="#7ea3d1"
              fillOpacity="0.18"
              stroke="#a8cbf2"
              strokeWidth="1.3"
            />
            <line
              x1="546"
              y1="401"
              x2="556"
              y2="401"
              stroke="#a8cbf2"
              strokeWidth="1"
              strokeOpacity="0.8"
            />
            <text
              x="551"
              y="428"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="15"
              fontWeight="700"
              fill="#dbe6f7"
            >
              Stewardship
            </text>
          </g>

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="233"
              y="199"
              width="264"
              height="144"
              rx="18"
              fill={`url(#${ids.trustFill})`}
              stroke="#7ea3d1"
              strokeWidth="1.3"
              strokeOpacity="0.65"
            />
            <rect
              x="233"
              y="199"
              width="264"
              height="1"
              fill="#ffffff"
              opacity="0.08"
            />
            <circle
              cx="365"
              cy="232"
              r="12"
              fill="none"
              stroke="#a8cbf2"
              strokeWidth="1.4"
            />
            <path
              d="M359,232 l4,5 l9,-11"
              fill="none"
              stroke="#a8cbf2"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
            <text
              x="365"
              y="272"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="22"
              fontWeight="700"
              fill="#f3f7fd"
            >
              Trust
            </text>
            <circle cx="365" cy="282" r="2.2" fill="#a8cbf2" />
            <text
              x="365"
              y="300"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="12"
              fontWeight="500"
              fill="#9fc4f0"
              letterSpacing="0.5"
            >
              Platform principle
            </text>
          </g>

          <text
            x="40"
            y="510"
            fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
            fontSize="13"
            fontWeight="400"
            fill="#7c8ca8"
          >
            Security · Privacy · Identity · Stewardship — confidence by design
          </text>

          <path
            d="M30,20 Q365,4 700,20"
            fill="none"
            stroke={`url(#${ids.hilite})`}
            strokeWidth="1.2"
          />
          <g stroke="#ffffff" strokeOpacity="0.16" strokeWidth="1">
            <path d="M28,52 L28,28 L52,28" fill="none" />
            <path d="M702,52 L702,28 L678,28" fill="none" />
            <path d="M28,508 L28,532 L52,532" fill="none" />
            <path d="M702,508 L702,532 L678,532" fill="none" />
          </g>
          <rect
            x="10.5"
            y="10.5"
            width="709"
            height="529"
            rx="19.5"
            fill="none"
            stroke="#ffffff"
            strokeOpacity="0.08"
          />
        </g>
      </svg>
    </div>
  );
}
