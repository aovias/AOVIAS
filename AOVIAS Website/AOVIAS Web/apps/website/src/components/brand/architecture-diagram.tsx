import { useId } from 'react';

import { cn } from '@/lib/utils';

export type ArchitectureDiagramProps = {
  title?: string;
  className?: string;
};

/**
 * Architecture stack — Experience → Control / Delivery → Shared platform foundation.
 */
export function ArchitectureDiagram({
  title = 'AOVIAS architecture stack illustration',
  className,
}: ArchitectureDiagramProps) {
  const uid = useId().replace(/:/g, '');
  const ids = {
    bgGrad: `arch-bg-${uid}`,
    barFill: `arch-bar-${uid}`,
    baseFill: `arch-base-${uid}`,
    topGlow: `arch-top-${uid}`,
    baseGlow: `arch-glow-${uid}`,
    nodeGlow: `arch-node-${uid}`,
    hilite: `arch-hi-${uid}`,
    grid: `arch-grid-${uid}`,
    clip: `arch-clip-${uid}`,
    shadow: `arch-shadow-${uid}`,
  };

  return (
    <div
      role="img"
      aria-label={title}
      className={cn(
        'relative w-full overflow-hidden rounded-2xl',
        'shadow-[0_0_28px_rgba(74,114,173,0.22)]',
        'aspect-[900/550] min-h-[12rem] xl:min-h-[16rem]',
        className,
      )}
    >
      <svg
        viewBox="0 0 900 550"
        xmlns="http://www.w3.org/2000/svg"
        className="size-full"
        role="img"
        aria-hidden
      >
        <title>{title}</title>
        <desc>
          Experience layer connects down into Control plane and Delivery plane,
          which both connect down into a Shared platform foundation, each layer
          carrying a small function icon, on a dark navy card with grid
          background.
        </desc>
        <defs>
          <linearGradient id={ids.bgGrad} x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#111a2c" />
            <stop offset="1" stopColor="#0a0e17" />
          </linearGradient>
          <linearGradient id={ids.barFill} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#242e48" />
            <stop offset="1" stopColor="#1a2338" />
          </linearGradient>
          <linearGradient id={ids.baseFill} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#3a4a70" />
            <stop offset="1" stopColor="#28324e" />
          </linearGradient>
          <radialGradient id={ids.topGlow} cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#4a72ad" stopOpacity="0.12" />
            <stop offset="1" stopColor="#4a72ad" stopOpacity="0" />
          </radialGradient>
          <radialGradient id={ids.baseGlow} cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#6f93c2" stopOpacity="0.22" />
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
            <rect x="10" y="10" width="880" height="530" rx="20" />
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
            width="880"
            height="530"
            fill={`url(#${ids.bgGrad})`}
          />
          <rect
            x="10"
            y="10"
            width="880"
            height="530"
            fill={`url(#${ids.grid})`}
          />
          <line
            x1="700"
            y1="10"
            x2="890"
            y2="190"
            stroke="#ffffff"
            strokeOpacity="0.035"
            strokeWidth="1"
          />
          <line
            x1="770"
            y1="10"
            x2="890"
            y2="120"
            stroke="#ffffff"
            strokeOpacity="0.03"
            strokeWidth="1"
          />
          <circle cx="450" cy="30" r="260" fill={`url(#${ids.topGlow})`} />
          <ellipse
            cx="450"
            cy="407"
            rx="420"
            ry="120"
            fill={`url(#${ids.baseGlow})`}
          />

          <line
            x1="450"
            y1="164"
            x2="450"
            y2="182"
            stroke="#8fb3e0"
            strokeOpacity="0.45"
            strokeWidth="1.5"
          />
          <path
            d="M450,182 L247.5,182 L247.5,200"
            fill="none"
            stroke="#8fb3e0"
            strokeOpacity="0.45"
            strokeWidth="1.5"
          />
          <path
            d="M450,182 L652.5,182 L652.5,200"
            fill="none"
            stroke="#8fb3e0"
            strokeOpacity="0.45"
            strokeWidth="1.5"
          />
          <circle cx="450" cy="182" r="7" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="450" cy="182" r="2.6" fill="#cfe0f5" />
          <circle cx="247.5" cy="200" r="2.4" fill="#8fb3e0" opacity="0.7" />
          <circle cx="652.5" cy="200" r="2.4" fill="#8fb3e0" opacity="0.7" />

          <line
            x1="247.5"
            y1="290"
            x2="247.5"
            y2="360"
            stroke="#8fb3e0"
            strokeOpacity="0.45"
            strokeWidth="1.5"
          />
          <circle cx="247.5" cy="325" r="7" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="247.5" cy="325" r="2.6" fill="#cfe0f5" />
          <line
            x1="652.5"
            y1="290"
            x2="652.5"
            y2="360"
            stroke="#8fb3e0"
            strokeOpacity="0.45"
            strokeWidth="1.5"
          />
          <circle cx="652.5" cy="325" r="7" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="652.5" cy="325" r="2.6" fill="#cfe0f5" />

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="70"
              y="80"
              width="760"
              height="84"
              rx="14"
              fill={`url(#${ids.barFill})`}
              stroke="#5b7aa8"
              strokeWidth="1"
              strokeOpacity="0.35"
            />
            <rect
              x="410"
              y="97"
              width="80"
              height="18"
              rx="3"
              fill="none"
              stroke="#9fc4f0"
              strokeWidth="1.3"
            />
            <line
              x1="410"
              y1="103"
              x2="490"
              y2="103"
              stroke="#9fc4f0"
              strokeWidth="1"
              strokeOpacity="0.6"
            />
            <circle cx="417" cy="100" r="1.3" fill="#9fc4f0" opacity="0.8" />
            <text
              x="450"
              y="144"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="17"
              fontWeight="700"
              fill="#dbe6f7"
            >
              Experience layer
            </text>
          </g>

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="70"
              y="200"
              width="355"
              height="90"
              rx="14"
              fill={`url(#${ids.barFill})`}
              stroke="#5b7aa8"
              strokeWidth="1"
              strokeOpacity="0.35"
            />
            <circle
              cx="247.5"
              cy="227"
              r="10"
              fill="none"
              stroke="#9fc4f0"
              strokeWidth="1.3"
            />
            <line
              x1="247.5"
              y1="227"
              x2="254"
              y2="220"
              stroke="#9fc4f0"
              strokeWidth="1.4"
            />
            <line
              x1="247.5"
              y1="215"
              x2="247.5"
              y2="219"
              stroke="#9fc4f0"
              strokeWidth="1.2"
              opacity="0.7"
            />
            <line
              x1="235"
              y1="233"
              x2="238"
              y2="230"
              stroke="#9fc4f0"
              strokeWidth="1.2"
              opacity="0.7"
            />
            <line
              x1="260"
              y1="233"
              x2="257"
              y2="230"
              stroke="#9fc4f0"
              strokeWidth="1.2"
              opacity="0.7"
            />
            <text
              x="247.5"
              y="264"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="16"
              fontWeight="700"
              fill="#dbe6f7"
            >
              Control plane
            </text>
          </g>

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="475"
              y="200"
              width="355"
              height="90"
              rx="14"
              fill={`url(#${ids.barFill})`}
              stroke="#5b7aa8"
              strokeWidth="1"
              strokeOpacity="0.35"
            />
            <circle cx="652.5" cy="227" r="2.6" fill="#bcd7f5" />
            <line
              x1="652.5"
              y1="227"
              x2="640"
              y2="219"
              stroke="#9fc4f0"
              strokeWidth="1.2"
              opacity="0.75"
            />
            <line
              x1="652.5"
              y1="227"
              x2="665"
              y2="219"
              stroke="#9fc4f0"
              strokeWidth="1.2"
              opacity="0.75"
            />
            <line
              x1="652.5"
              y1="227"
              x2="652.5"
              y2="238"
              stroke="#9fc4f0"
              strokeWidth="1.2"
              opacity="0.75"
            />
            <circle cx="640" cy="219" r="1.6" fill="#9fc4f0" />
            <circle cx="665" cy="219" r="1.6" fill="#9fc4f0" />
            <circle cx="652.5" cy="238" r="1.6" fill="#9fc4f0" />
            <text
              x="652.5"
              y="264"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="16"
              fontWeight="700"
              fill="#dbe6f7"
            >
              Delivery plane
            </text>
          </g>

          <g filter={`url(#${ids.shadow})`}>
            <rect
              x="70"
              y="360"
              width="760"
              height="94"
              rx="16"
              fill={`url(#${ids.baseFill})`}
              stroke="#7ea3d1"
              strokeWidth="1.2"
              strokeOpacity="0.55"
            />
            <rect
              x="70"
              y="360"
              width="760"
              height="1"
              fill={`url(#${ids.hilite})`}
            />
            <line
              x1="438"
              y1="382"
              x2="462"
              y2="382"
              stroke="#dbe9fb"
              strokeWidth="2"
              opacity="0.9"
            />
            <line
              x1="432"
              y1="388"
              x2="468"
              y2="388"
              stroke="#dbe9fb"
              strokeWidth="2"
              opacity="0.65"
            />
            <line
              x1="426"
              y1="394"
              x2="474"
              y2="394"
              stroke="#dbe9fb"
              strokeWidth="2"
              opacity="0.4"
            />
            <text
              x="450"
              y="430"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="18"
              fontWeight="700"
              fill="#f2f6fc"
            >
              Shared platform foundation
            </text>
          </g>

          <path
            d="M30,20 Q450,4 870,20"
            fill="none"
            stroke={`url(#${ids.hilite})`}
            strokeWidth="1.2"
          />
          <g stroke="#ffffff" strokeOpacity="0.16" strokeWidth="1">
            <path d="M28,52 L28,28 L52,28" fill="none" />
            <path d="M872,52 L872,28 L848,28" fill="none" />
            <path d="M28,508 L28,532 L52,532" fill="none" />
            <path d="M872,508 L872,532 L848,532" fill="none" />
          </g>
          <rect
            x="10.5"
            y="10.5"
            width="879"
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
