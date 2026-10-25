# Compatibility redirect — production image definition lives at:
#   deployment/docker/Dockerfile
#
# Build with:
#   docker build -f deployment/docker/Dockerfile --target runner -t aovias-website:r1 .
#
# This file intentionally fails if used as a build target so callers migrate.
# syntax=docker/dockerfile:1
FROM alpine:3.20 AS deprecated
RUN printf '%s\n' \
  'ERROR: docker/Dockerfile is deprecated.' \
  'Use: deployment/docker/Dockerfile' \
  && exit 1
