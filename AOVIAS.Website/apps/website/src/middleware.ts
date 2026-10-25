import { NextResponse, type NextRequest } from 'next/server';

const MAINTENANCE_BYPASS = new Set([
  '/maintenance',
  '/api/health',
  '/api/ready',
  '/robots.txt',
  '/sitemap.xml',
]);

function isProductionAppEnv(): boolean {
  const appEnv = process.env['NEXT_PUBLIC_APP_ENV'];
  return appEnv === 'production' || process.env['NODE_ENV'] === 'production';
}

/**
 * Internal / preview shells must not be public in production.
 */
function isInternalSurface(pathname: string): boolean {
  if (pathname.startsWith('/dev') || pathname.startsWith('/internal')) {
    return true;
  }
  if (pathname.endsWith('/preview') || pathname.includes('/preview/')) {
    return true;
  }
  return false;
}

/**
 * Edge middleware — maintenance mode + production surface hardening.
 */
export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  if (isProductionAppEnv() && isInternalSurface(pathname)) {
    // Rewrite to a non-route so App Router renders the shared 404 UI.
    const url = request.nextUrl.clone();
    url.pathname = '/__internal_unavailable';
    return NextResponse.rewrite(url);
  }

  const maintenance = process.env['MAINTENANCE_MODE'] === 'true';

  if (
    maintenance &&
    !MAINTENANCE_BYPASS.has(pathname) &&
    !pathname.startsWith('/_next') &&
    !pathname.startsWith('/icons') &&
    !pathname.startsWith('/brand')
  ) {
    const url = request.nextUrl.clone();
    url.pathname = '/maintenance';
    return NextResponse.rewrite(url);
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    '/((?!_next/static|_next/image|favicon.ico|.*\\.(?:svg|png|jpg|jpeg|gif|webp)$).*)',
  ],
};
