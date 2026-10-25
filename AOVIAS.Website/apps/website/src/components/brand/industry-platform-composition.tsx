import { useId } from 'react';

import { cn } from '@/lib/utils';

export type IndustryPlatformCompositionProps = {
  title?: string;
  /** Retained for API compatibility; caption is rendered inside the SVG. */
  caption?: string;
  className?: string;
};

/**
 * One platform serving multiple industries — Healthcare · Government ·
 * Manufacturing · Financial Services around AOVIAS Platform hub.
 */
export function IndustryPlatformComposition({
  title = 'AOVIAS industries illustration',
  className,
}: IndustryPlatformCompositionProps) {
  const uid = useId().replace(/:/g, '');
  const ids = {
    bgGrad: `ind-bg-${uid}`,
    boxFill: `ind-box-${uid}`,
    platFill: `ind-plat-${uid}`,
    topGlow: `ind-top-${uid}`,
    platGlow: `ind-pg-${uid}`,
    nodeGlow: `ind-node-${uid}`,
    hilite: `ind-hi-${uid}`,
    grid: `ind-grid-${uid}`,
    clip: `ind-clip-${uid}`,
    shadow: `ind-shadow-${uid}`,
  };

  return (
    <div
      role="img"
      aria-label={title}
      className={cn(
        'relative w-full overflow-hidden rounded-2xl',
        'shadow-[0_0_28px_rgba(74,114,173,0.22)]',
        'aspect-[860/550] min-h-[14rem] xl:min-h-[18rem]',
        className,
      )}
    >
      <svg
        viewBox="0 0 860 550"
        xmlns="http://www.w3.org/2000/svg"
        className="size-full"
        role="img"
        aria-hidden
      >
        <title>{title}</title>
        <desc>
          Healthcare, Government, Manufacturing and Financial Services boxes
          connect diagonally into a central AOVIAS Platform hub labelled Shared
          foundation, each industry carrying its own icon, on a dark navy card
          with grid background.
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
          <linearGradient id={ids.platFill} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#3d4d74" />
            <stop offset="1" stopColor="#28324e" />
          </linearGradient>
          <radialGradient id={ids.topGlow} cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#4a72ad" stopOpacity="0.12" />
            <stop offset="1" stopColor="#4a72ad" stopOpacity="0" />
          </radialGradient>
          <radialGradient id={ids.platGlow} cx="0.5" cy="0.5" r="0.5">
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
            <rect x="10" y="10" width="840" height="530" rx="20" />
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
            width="840"
            height="530"
            fill={`url(#${ids.bgGrad})`}
          />
          <rect
            x="10"
            y="10"
            width="840"
            height="530"
            fill={`url(#${ids.grid})`}
          />
          <line
            x1="680"
            y1="10"
            x2="850"
            y2="170"
            stroke="#ffffff"
            strokeOpacity="0.035"
            strokeWidth="1"
          />
          <line
            x1="740"
            y1="10"
            x2="850"
            y2="110"
            stroke="#ffffff"
            strokeOpacity="0.03"
            strokeWidth="1"
          />
          <circle cx="430" cy="20" r="250" fill={`url(#${ids.topGlow})`} />
          <circle cx="430" cy="253" r="220" fill={`url(#${ids.platGlow})`} />
          <ellipse
            cx="430"
            cy="253"
            rx="192"
            ry="112"
            fill="none"
            stroke="#8fb3e0"
            strokeOpacity="0.2"
            strokeWidth="1"
            strokeDasharray="3 7"
          />

          <line
            x1="277"
            y1="168"
            x2="306"
            y2="200"
            stroke="#8fb3e0"
            strokeOpacity="0.45"
            strokeWidth="1.4"
          />
          <circle cx="291.5" cy="184" r="6" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="291.5" cy="184" r="2.2" fill="#cfe0f5" />
          <line
            x1="578"
            y1="168"
            x2="554"
            y2="200"
            stroke="#8fb3e0"
            strokeOpacity="0.45"
            strokeWidth="1.4"
          />
          <circle cx="566" cy="184" r="6" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="566" cy="184" r="2.2" fill="#cfe0f5" />
          <line
            x1="277"
            y1="354"
            x2="306"
            y2="307"
            stroke="#8fb3e0"
            strokeOpacity="0.45"
            strokeWidth="1.4"
          />
          <circle cx="291.5" cy="330.5" r="6" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="291.5" cy="330.5" r="2.2" fill="#cfe0f5" />
          <line
            x1="578"
            y1="354"
            x2="554"
            y2="307"
            stroke="#8fb3e0"
            strokeOpacity="0.45"
            strokeWidth="1.4"
          />
          <circle cx="566" cy="330.5" r="6" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="566" cy="330.5" r="2.2" fill="#cfe0f5" />

          <line
            x1="283"
            y1="127"
            x2="303"
            y2="127"
            stroke="#8fb3e0"
            strokeOpacity="0.3"
            strokeWidth="1.3"
          />
          <line
            x1="575"
            y1="127"
            x2="555"
            y2="127"
            stroke="#8fb3e0"
            strokeOpacity="0.3"
            strokeWidth="1.3"
          />
          <line
            x1="283"
            y1="395"
            x2="303"
            y2="395"
            stroke="#8fb3e0"
            strokeOpacity="0.3"
            strokeWidth="1.3"
          />
          <line
            x1="575"
            y1="395"
            x2="555"
            y2="395"
            stroke="#8fb3e0"
            strokeOpacity="0.3"
            strokeWidth="1.3"
          />

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="95"
              y="86"
              width="182"
              height="82"
              rx="14"
              fill={`url(#${ids.boxFill})`}
              stroke="#5b7aa8"
              strokeWidth="1"
              strokeOpacity="0.35"
            />
            <rect x="177" y="101" width="5" height="18" rx="1.5" fill="#a8cbf2" />
            <rect
              x="168.5"
              y="107.5"
              width="18"
              height="5"
              rx="1.5"
              fill="#a8cbf2"
            />
            <text
              x="186"
              y="144"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="15"
              fontWeight="700"
              fill="#dbe6f7"
            >
              Healthcare
            </text>
          </g>

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="578"
              y="86"
              width="184"
              height="82"
              rx="14"
              fill={`url(#${ids.boxFill})`}
              stroke="#5b7aa8"
              strokeWidth="1"
              strokeOpacity="0.35"
            />
            <path
              d="M660,106 L670,96 L680,106"
              fill="none"
              stroke="#a8cbf2"
              strokeWidth="1.3"
            />
            <line
              x1="659"
              y1="112"
              x2="681"
              y2="112"
              stroke="#a8cbf2"
              strokeWidth="1.3"
            />
            <line
              x1="664"
              y1="107"
              x2="664"
              y2="111"
              stroke="#a8cbf2"
              strokeWidth="1.2"
              opacity="0.8"
            />
            <line
              x1="670"
              y1="107"
              x2="670"
              y2="111"
              stroke="#a8cbf2"
              strokeWidth="1.2"
              opacity="0.8"
            />
            <line
              x1="676"
              y1="107"
              x2="676"
              y2="111"
              stroke="#a8cbf2"
              strokeWidth="1.2"
              opacity="0.8"
            />
            <text
              x="670"
              y="144"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="15"
              fontWeight="700"
              fill="#dbe6f7"
            >
              Government
            </text>
          </g>

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="95"
              y="354"
              width="182"
              height="82"
              rx="14"
              fill={`url(#${ids.boxFill})`}
              stroke="#5b7aa8"
              strokeWidth="1"
              strokeOpacity="0.35"
            />
            <circle
              cx="186"
              cy="378"
              r="7"
              fill="none"
              stroke="#a8cbf2"
              strokeWidth="1.3"
            />
            <g stroke="#a8cbf2" strokeWidth="1.2" opacity="0.8">
              <line x1="186" y1="368" x2="186" y2="371" />
              <line x1="186" y1="385" x2="186" y2="388" />
              <line x1="176" y1="378" x2="179" y2="378" />
              <line x1="193" y1="378" x2="196" y2="378" />
              <line x1="179.1" y1="371.1" x2="181" y2="373" />
              <line x1="192.9" y1="384.9" x2="191" y2="383" />
            </g>
            <text
              x="186"
              y="412"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="15"
              fontWeight="700"
              fill="#dbe6f7"
            >
              Manufacturing
            </text>
          </g>

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="578"
              y="354"
              width="184"
              height="82"
              rx="14"
              fill={`url(#${ids.boxFill})`}
              stroke="#5b7aa8"
              strokeWidth="1"
              strokeOpacity="0.35"
            />
            <rect
              x="660"
              y="377"
              width="5"
              height="10"
              fill="#a8cbf2"
              opacity="0.75"
            />
            <rect
              x="667.5"
              y="373"
              width="5"
              height="14"
              fill="#a8cbf2"
              opacity="0.85"
            />
            <rect x="675" y="369" width="5" height="18" fill="#a8cbf2" />
            <text
              x="670"
              y="412"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="15"
              fontWeight="700"
              fill="#dbe6f7"
            >
              Financial Services
            </text>
          </g>

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="306"
              y="200"
              width="248"
              height="107"
              rx="18"
              fill={`url(#${ids.platFill})`}
              stroke="#7ea3d1"
              strokeWidth="1.3"
              strokeOpacity="0.65"
            />
            <rect
              x="306"
              y="200"
              width="248"
              height="1"
              fill="#ffffff"
              opacity="0.08"
            />
            <text
              x="430"
              y="243"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="19"
              fontWeight="700"
              fill="#f3f7fd"
            >
              AOVIAS Platform
            </text>
            <circle cx="430" cy="253" r="2.4" fill="#a8cbf2" />
            <text
              x="430"
              y="273"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="12"
              fontWeight="500"
              fill="#9fc4f0"
              letterSpacing="0.5"
            >
              Shared foundation
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
            Healthcare · Government · Manufacturing · Financial Services
          </text>

          <path
            d="M30,20 Q430,4 830,20"
            fill="none"
            stroke={`url(#${ids.hilite})`}
            strokeWidth="1.2"
          />
          <g stroke="#ffffff" strokeOpacity="0.16" strokeWidth="1">
            <path d="M28,52 L28,28 L52,28" fill="none" />
            <path d="M832,52 L832,28 L808,28" fill="none" />
            <path d="M28,508 L28,532 L52,532" fill="none" />
            <path d="M832,508 L832,532 L808,532" fill="none" />
          </g>
          <rect
            x="10.5"
            y="10.5"
            width="839"
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
