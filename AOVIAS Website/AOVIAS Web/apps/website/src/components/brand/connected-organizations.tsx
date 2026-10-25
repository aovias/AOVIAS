import { useId } from 'react';

import { cn } from '@/lib/utils';

export type ConnectedOrganizationsProps = {
  title?: string;
  /** Retained for API compatibility; caption is rendered inside the SVG. */
  caption?: string;
  className?: string;
};

/**
 * Platform orchestration — Institution · AOVIAS Platform · Partner
 * over Shared Interoperability Layer.
 */
export function ConnectedOrganizations({
  title = 'AOVIAS platform orchestration illustration',
  className,
}: ConnectedOrganizationsProps) {
  const uid = useId().replace(/:/g, '');
  const ids = {
    bgGrad: `co-bg-${uid}`,
    boxFill: `co-box-${uid}`,
    platFill: `co-plat-${uid}`,
    barFill: `co-bar-${uid}`,
    topGlow: `co-top-${uid}`,
    platGlow: `co-pg-${uid}`,
    nodeGlow: `co-node-${uid}`,
    spineGrad: `co-spine-${uid}`,
    dots: `co-dots-${uid}`,
    cardClip: `co-clip-${uid}`,
    softShadow: `co-shadow-${uid}`,
  };

  return (
    <div
      role="img"
      aria-label={title}
      className={cn(
        'relative w-full overflow-hidden rounded-2xl',
        'shadow-[0_0_28px_rgba(74,114,173,0.22)]',
        'aspect-[4/3] min-h-[16rem] xl:min-h-[20rem]',
        className,
      )}
    >
      <svg
        viewBox="0 0 880 660"
        xmlns="http://www.w3.org/2000/svg"
        className="size-full"
        role="img"
        aria-hidden
      >
        <title>{title}</title>
        <desc>
          Institution Systems and Partner Ecosystem nodes connect into a central
          AOVIAS Platform core, which links down to a Shared Interoperability
          Layer, with satellite ecosystem markers and ambient glow on a dark navy
          card.
        </desc>
        <defs>
          <linearGradient id={ids.bgGrad} x1="0" y1="0" x2="1" y2="1">
            <stop offset="0" stopColor="#111a2c" />
            <stop offset="1" stopColor="#0a0e17" />
          </linearGradient>
          <linearGradient id={ids.boxFill} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#1a2337" />
            <stop offset="1" stopColor="#141d2f" />
          </linearGradient>
          <linearGradient id={ids.platFill} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="#26314f" />
            <stop offset="1" stopColor="#182036" />
          </linearGradient>
          <linearGradient id={ids.barFill} x1="0" y1="0" x2="1" y2="0">
            <stop offset="0" stopColor="#161f34" />
            <stop offset="0.5" stopColor="#1c2740" />
            <stop offset="1" stopColor="#161f34" />
          </linearGradient>
          <radialGradient id={ids.topGlow} cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#4a72ad" stopOpacity="0.12" />
            <stop offset="1" stopColor="#4a72ad" stopOpacity="0" />
          </radialGradient>
          <radialGradient id={ids.platGlow} cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#6f93c2" stopOpacity="0.28" />
            <stop offset="1" stopColor="#6f93c2" stopOpacity="0" />
          </radialGradient>
          <radialGradient id={ids.nodeGlow} cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" stopColor="#a8cbf2" stopOpacity="0.55" />
            <stop offset="1" stopColor="#a8cbf2" stopOpacity="0" />
          </radialGradient>
          <linearGradient id={ids.spineGrad} x1="0" y1="0" x2="1" y2="0">
            <stop offset="0" stopColor="#8fb3e0" stopOpacity="0.5" />
            <stop offset="1" stopColor="#8fb3e0" stopOpacity="0.5" />
          </linearGradient>
          <pattern
            id={ids.dots}
            width="28"
            height="28"
            patternUnits="userSpaceOnUse"
          >
            <circle cx="1" cy="1" r="1" fill="#8fb3e0" fillOpacity="0.06" />
          </pattern>
          <clipPath id={ids.cardClip}>
            <rect x="0" y="0" width="880" height="660" rx="20" />
          </clipPath>
          <filter
            id={ids.softShadow}
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

        <g clipPath={`url(#${ids.cardClip})`}>
          <rect
            x="0"
            y="0"
            width="880"
            height="660"
            fill={`url(#${ids.bgGrad})`}
          />
          <rect
            x="0"
            y="0"
            width="880"
            height="660"
            fill={`url(#${ids.dots})`}
          />

          <line
            x1="700"
            y1="0"
            x2="880"
            y2="170"
            stroke="#ffffff"
            strokeOpacity="0.035"
            strokeWidth="1"
          />
          <line
            x1="770"
            y1="0"
            x2="880"
            y2="100"
            stroke="#ffffff"
            strokeOpacity="0.03"
            strokeWidth="1"
          />
          <line
            x1="0"
            y1="660"
            x2="150"
            y2="550"
            stroke="#ffffff"
            strokeOpacity="0.03"
            strokeWidth="1"
          />

          <circle cx="440" cy="20" r="260" fill={`url(#${ids.topGlow})`} />
          <circle cx="440" cy="260" r="230" fill={`url(#${ids.platGlow})`} />

          <circle cx="30" cy="222" r="2.5" fill="#7ea3d1" opacity="0.4" />
          <line
            x1="30"
            y1="222"
            x2="54"
            y2="280"
            stroke="#7ea3d1"
            strokeOpacity="0.18"
            strokeWidth="1"
            strokeDasharray="2 4"
          />
          <circle cx="30" cy="356" r="2.5" fill="#7ea3d1" opacity="0.4" />
          <line
            x1="30"
            y1="356"
            x2="54"
            y2="354"
            stroke="#7ea3d1"
            strokeOpacity="0.18"
            strokeWidth="1"
            strokeDasharray="2 4"
          />
          <circle cx="850" cy="222" r="2.5" fill="#7ea3d1" opacity="0.4" />
          <line
            x1="850"
            y1="222"
            x2="826"
            y2="280"
            stroke="#7ea3d1"
            strokeOpacity="0.18"
            strokeWidth="1"
            strokeDasharray="2 4"
          />
          <circle cx="850" cy="356" r="2.5" fill="#7ea3d1" opacity="0.4" />
          <line
            x1="850"
            y1="356"
            x2="826"
            y2="354"
            stroke="#7ea3d1"
            strokeOpacity="0.18"
            strokeWidth="1"
            strokeDasharray="2 4"
          />

          <line
            x1="244"
            y1="317"
            x2="284"
            y2="317"
            stroke="#8fb3e0"
            strokeOpacity="0.5"
            strokeWidth="1.5"
          />
          <circle cx="252" cy="317" r="1.4" fill="#8fb3e0" opacity="0.35" />
          <circle cx="264" cy="317" r="9" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="264" cy="317" r="3.2" fill="#cfe0f5" />
          <circle cx="276" cy="317" r="1.4" fill="#8fb3e0" opacity="0.35" />

          <line
            x1="596"
            y1="317"
            x2="636"
            y2="317"
            stroke="#8fb3e0"
            strokeOpacity="0.5"
            strokeWidth="1.5"
          />
          <circle cx="604" cy="317" r="1.4" fill="#8fb3e0" opacity="0.35" />
          <circle cx="616" cy="317" r="9" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="616" cy="317" r="3.2" fill="#cfe0f5" />
          <circle cx="628" cy="317" r="1.4" fill="#8fb3e0" opacity="0.35" />

          <line
            x1="440"
            y1="442"
            x2="440"
            y2="520"
            stroke="#8fb3e0"
            strokeOpacity="0.5"
            strokeWidth="1.5"
          />
          <circle cx="440" cy="465" r="1.4" fill="#8fb3e0" opacity="0.35" />
          <circle cx="440" cy="481" r="9" fill={`url(#${ids.nodeGlow})`} />
          <circle cx="440" cy="481" r="3.2" fill="#cfe0f5" />
          <circle cx="440" cy="497" r="1.4" fill="#8fb3e0" opacity="0.35" />

          <g filter={`url(#${ids.softShadow})`}>
            <rect
              x="54"
              y="258"
              width="190"
              height="118"
              rx="14"
              fill={`url(#${ids.boxFill})`}
              stroke="#5b7aa8"
              strokeWidth="1"
              strokeOpacity="0.35"
            />
            <path
              d="M127,286 L149,274 L171,286"
              fill="none"
              stroke="#8fb3e0"
              strokeWidth="1.3"
              strokeOpacity="0.8"
            />
            <line
              x1="125"
              y1="288"
              x2="173"
              y2="288"
              stroke="#8fb3e0"
              strokeWidth="1.4"
              strokeOpacity="0.8"
            />
            <line
              x1="132"
              y1="290"
              x2="132"
              y2="304"
              stroke="#8fb3e0"
              strokeWidth="1.4"
              strokeOpacity="0.7"
            />
            <line
              x1="141"
              y1="290"
              x2="141"
              y2="304"
              stroke="#8fb3e0"
              strokeWidth="1.4"
              strokeOpacity="0.7"
            />
            <line
              x1="157"
              y1="290"
              x2="157"
              y2="304"
              stroke="#8fb3e0"
              strokeWidth="1.4"
              strokeOpacity="0.7"
            />
            <line
              x1="166"
              y1="290"
              x2="166"
              y2="304"
              stroke="#8fb3e0"
              strokeWidth="1.4"
              strokeOpacity="0.7"
            />
            <line
              x1="123"
              y1="306"
              x2="175"
              y2="306"
              stroke="#8fb3e0"
              strokeWidth="1.4"
              strokeOpacity="0.8"
            />
            <text
              x="149"
              y="330"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="16"
              fontWeight="700"
              fill="#dbe6f7"
            >
              Institution
            </text>
            <text
              x="149"
              y="352"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="12"
              fontWeight="500"
              fill="#8ea3c4"
              letterSpacing="0.3"
            >
              Systems
            </text>
          </g>

          <g filter={`url(#${ids.softShadow})`}>
            <rect
              x="636"
              y="258"
              width="190"
              height="118"
              rx="14"
              fill={`url(#${ids.boxFill})`}
              stroke="#5b7aa8"
              strokeWidth="1"
              strokeOpacity="0.35"
            />
            <line
              x1="731"
              y1="278"
              x2="713"
              y2="304"
              stroke="#8fb3e0"
              strokeWidth="1.2"
              strokeOpacity="0.7"
            />
            <line
              x1="731"
              y1="278"
              x2="749"
              y2="304"
              stroke="#8fb3e0"
              strokeWidth="1.2"
              strokeOpacity="0.7"
            />
            <line
              x1="713"
              y1="304"
              x2="749"
              y2="304"
              stroke="#8fb3e0"
              strokeWidth="1.2"
              strokeOpacity="0.7"
            />
            <circle cx="731" cy="278" r="5" fill="#bcd7f5" />
            <circle cx="713" cy="304" r="4" fill="#9fc4f0" />
            <circle cx="749" cy="304" r="4" fill="#9fc4f0" />
            <text
              x="731"
              y="330"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="16"
              fontWeight="700"
              fill="#dbe6f7"
            >
              Partner
            </text>
            <text
              x="731"
              y="352"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="12"
              fontWeight="500"
              fill="#8ea3c4"
              letterSpacing="0.3"
            >
              Ecosystem
            </text>
          </g>

          <g filter={`url(#${ids.softShadow})`}>
            <rect
              x="284"
              y="192"
              width="312"
              height="250"
              rx="18"
              fill={`url(#${ids.platFill})`}
              stroke="#7ea3d1"
              strokeWidth="1.3"
              strokeOpacity="0.65"
            />
            <rect
              x="284"
              y="192"
              width="312"
              height="1"
              fill="#ffffff"
              opacity="0.08"
            />
            <path
              d="M415,262 L440,222 L465,262 Z"
              fill="none"
              stroke="#9fc4f0"
              strokeWidth="1.6"
            />
            <line
              x1="427"
              y1="245"
              x2="453"
              y2="245"
              stroke="#9fc4f0"
              strokeWidth="1"
              strokeOpacity="0.5"
            />
            <circle cx="440" cy="222" r="7" fill={`url(#${ids.nodeGlow})`} />
            <circle cx="440" cy="222" r="3" fill="#dbe9fb" />
            <text
              x="440"
              y="302"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="24"
              fontWeight="700"
              fill="#f3f7fd"
              letterSpacing="0.5"
            >
              AOVIAS
            </text>
            <text
              x="440"
              y="326"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="12"
              fontWeight="600"
              fill="#9fc4f0"
              letterSpacing="3"
            >
              PLATFORM
            </text>
            <line
              x1="390"
              y1="344"
              x2="490"
              y2="344"
              stroke="#ffffff"
              strokeOpacity="0.1"
              strokeWidth="1"
            />
            <rect
              x="340"
              y="364"
              width="200"
              height="30"
              rx="15"
              fill="#7ea3d1"
              fillOpacity="0.12"
              stroke="#7ea3d1"
              strokeOpacity="0.4"
              strokeWidth="1"
            />
            <text
              x="440"
              y="384"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="12"
              fontWeight="500"
              fill="#cfe0f5"
            >
              Accountability · Readiness
            </text>
          </g>

          <g filter={`url(#${ids.softShadow})`}>
            <rect
              x="66"
              y="520"
              width="748"
              height="56"
              rx="12"
              fill={`url(#${ids.barFill})`}
              stroke="#7ea3d1"
              strokeOpacity="0.3"
              strokeWidth="1"
            />
            <rect
              x="66"
              y="520"
              width="748"
              height="1"
              fill="#ffffff"
              opacity="0.08"
            />
            <g stroke="#8fb3e0" strokeOpacity="0.06" strokeWidth="1">
              <line x1="86" y1="528" x2="86" y2="568" />
              <line x1="102" y1="528" x2="102" y2="568" />
              <line x1="118" y1="528" x2="118" y2="568" />
              <line x1="134" y1="528" x2="134" y2="568" />
              <line x1="150" y1="528" x2="150" y2="568" />
              <line x1="166" y1="528" x2="166" y2="568" />
              <line x1="182" y1="528" x2="182" y2="568" />
              <line x1="714" y1="528" x2="714" y2="568" />
              <line x1="730" y1="528" x2="730" y2="568" />
              <line x1="746" y1="528" x2="746" y2="568" />
              <line x1="762" y1="528" x2="762" y2="568" />
              <line x1="778" y1="528" x2="778" y2="568" />
              <line x1="794" y1="528" x2="794" y2="568" />
            </g>
            <text
              x="440"
              y="546"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="13"
              fontWeight="700"
              fill="#dbe6f7"
              letterSpacing="2"
            >
              SHARED INTEROPERABILITY LAYER
            </text>
            <text
              x="440"
              y="564"
              textAnchor="middle"
              fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
              fontSize="11"
              fontWeight="500"
              fill="#8ea3c4"
            >
              Secure · Compliant · Durable
            </text>
          </g>

          <text
            x="66"
            y="606"
            fontFamily="-apple-system,Segoe UI,Helvetica,Arial,sans-serif"
            fontSize="13"
            fontWeight="400"
            fill="#7c8ca8"
          >
            Platform orchestration across institutions and partners
          </text>

          <g stroke="#ffffff" strokeOpacity="0.16" strokeWidth="1">
            <path d="M20,44 L20,20 L44,20" fill="none" />
            <path d="M860,44 L860,20 L836,20" fill="none" />
            <path d="M20,616 L20,640 L44,640" fill="none" />
            <path d="M860,616 L860,640 L836,640" fill="none" />
          </g>

          <rect
            x="0.5"
            y="0.5"
            width="879"
            height="659"
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
