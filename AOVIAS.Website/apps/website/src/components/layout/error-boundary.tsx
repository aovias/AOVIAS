'use client';

import { Component, type ErrorInfo, type ReactNode } from 'react';

import { getTelemetryClient } from '@/telemetry';

type ErrorBoundaryProps = {
  children: ReactNode;
  fallback?: ReactNode;
};

type ErrorBoundaryState = {
  hasError: boolean;
};

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  public override state: ErrorBoundaryState = { hasError: false };

  public static getDerivedStateFromError(): ErrorBoundaryState {
    return { hasError: true };
  }

  public override componentDidCatch(error: Error, info: ErrorInfo): void {
    console.error('Layout error boundary captured an error', error, info);
    getTelemetryClient().trackError({
      message: error.message,
      name: error.name,
      fatal: true,
      ...(error.stack ? { stack: error.stack } : {}),
      attributes: {
        'error.component_stack': info.componentStack ?? '',
      },
    });
  }

  public override render(): ReactNode {
    if (this.state.hasError) {
      return (
        this.props.fallback ?? (
          <div className="bg-error-subtle text-error rounded-md p-6" role="alert">
            <p className="font-medium">Something went wrong.</p>
            <p className="text-small mt-2">Please refresh the page or try again later.</p>
          </div>
        )
      );
    }

    return this.props.children;
  }
}
