# syntax=docker/dockerfile:1
# AOVIAS Public Website — production multi-stage image
# Build context: repository root
# Target `runner` for production; `development` for local compose.

FROM node:22-alpine AS base
WORKDIR /app
RUN apk add --no-cache libc6-compat

# ---------------------------------------------------------------------------
# Dependencies — package manifests only for better layer caching
# ---------------------------------------------------------------------------
FROM base AS deps
ENV HUSKY=0
COPY package.json package-lock.json ./
COPY apps/website/package.json ./apps/website/package.json
COPY packages/typescript-config/package.json ./packages/typescript-config/package.json
COPY packages/eslint-config/package.json ./packages/eslint-config/package.json
COPY packages/ui/package.json ./packages/ui/package.json
COPY packages/design-tokens/package.json ./packages/design-tokens/package.json
COPY packages/design-system/package.json ./packages/design-system/package.json
COPY packages/branding/package.json ./packages/branding/package.json
COPY packages/icons/package.json ./packages/icons/package.json
COPY packages/utilities/package.json ./packages/utilities/package.json
COPY packages/sdk/package.json ./packages/sdk/package.json
RUN npm ci

# ---------------------------------------------------------------------------
# Builder — compile Next.js standalone output
# ---------------------------------------------------------------------------
FROM base AS builder
ENV HUSKY=0
ENV NEXT_TELEMETRY_DISABLED=1
ENV NODE_ENV=production
COPY --from=deps /app/node_modules ./node_modules
COPY --from=deps /app/apps/website/node_modules ./apps/website/node_modules
COPY . .
ARG NEXT_PUBLIC_SITE_URL=https://aovias.com
ARG NEXT_PUBLIC_APP_ENV=production
ENV NEXT_PUBLIC_SITE_URL=$NEXT_PUBLIC_SITE_URL
ENV NEXT_PUBLIC_APP_ENV=$NEXT_PUBLIC_APP_ENV
RUN npm run build --workspace=@aovias/website

# ---------------------------------------------------------------------------
# Runner — minimal production image (non-root)
# ---------------------------------------------------------------------------
FROM base AS runner
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

RUN apk add --no-cache wget \
  && addgroup --system --gid 1001 nodejs \
  && adduser --system --uid 1001 nextjs

COPY --from=builder /app/apps/website/public ./apps/website/public
COPY --from=builder --chown=nextjs:nodejs /app/apps/website/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/apps/website/.next/static ./apps/website/.next/static

USER nextjs
EXPOSE 3000

HEALTHCHECK --interval=30s --timeout=5s --start-period=25s --retries=3 \
  CMD wget -qO- http://127.0.0.1:3000/api/health || exit 1

CMD ["node", "apps/website/server.js"]

# ---------------------------------------------------------------------------
# Development target (compose)
# ---------------------------------------------------------------------------
FROM base AS development
ENV HUSKY=0
COPY package.json package-lock.json ./
COPY apps/website/package.json ./apps/website/package.json
COPY packages ./packages
RUN npm ci
COPY . .
ENV NEXT_TELEMETRY_DISABLED=1
ENV NODE_ENV=development
EXPOSE 3000
CMD ["npm", "run", "dev", "--workspace=@aovias/website", "--", "--hostname", "0.0.0.0", "--port", "3000"]
