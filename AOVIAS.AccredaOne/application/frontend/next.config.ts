import type { NextConfig } from "next";

const apiProxyTarget = process.env.ACCREDAONE_API_PROXY_TARGET ?? "http://localhost:5022";

const nextConfig: NextConfig = {
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${apiProxyTarget}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
