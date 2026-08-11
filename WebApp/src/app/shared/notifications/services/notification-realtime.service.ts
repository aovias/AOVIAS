import { Injectable, inject, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { TokenService } from '../../../core/auth/token.service';
import { environment } from '../../../../environments/environment';

export type NotificationRealtimeHandler = (payload?: unknown) => void;

/**
 * Connects to Gateway NotificationHub (`/hubs/notifications`) via @microsoft/signalr.
 * Supports multiple subscribers (bell + notification center) with ref-counted disconnect.
 */
@Injectable({ providedIn: 'root' })
export class NotificationRealtimeService implements OnDestroy {
  private readonly tokenService = inject(TokenService);
  private readonly handlers = new Set<NotificationRealtimeHandler>();
  private connection: signalR.HubConnection | null = null;
  private starting: Promise<void> | null = null;

  async connect(onNotification: NotificationRealtimeHandler): Promise<void> {
    this.handlers.add(onNotification);
    if (this.connection?.state === signalR.HubConnectionState.Connected) {
      return;
    }
    if (this.starting) {
      await this.starting;
      return;
    }

    const token = this.tokenService.getAccessToken();
    if (!token) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(resolveNotificationHubUrl(), {
        accessTokenFactory: () => this.tokenService.getAccessToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.connection.on('notificationReceived', (payload: unknown) => {
      for (const handler of this.handlers) {
        handler(payload);
      }
    });

    this.starting = this.connection
      .start()
      .catch((err) => {
        console.warn('[NotificationRealtime] hub connect failed', err);
      })
      .finally(() => {
        this.starting = null;
      });

    await this.starting;
  }

  notifyReceived(payload?: unknown): void {
    for (const handler of this.handlers) {
      handler(payload);
    }
  }

  async disconnect(onNotification?: NotificationRealtimeHandler): Promise<void> {
    if (onNotification) {
      this.handlers.delete(onNotification);
    } else {
      this.handlers.clear();
    }

    if (this.handlers.size > 0) {
      return;
    }

    const conn = this.connection;
    this.connection = null;
    if (conn) {
      try {
        await conn.stop();
      } catch {
        /* ignore */
      }
    }
  }

  ngOnDestroy(): void {
    void this.disconnect();
  }
}

function resolveNotificationHubUrl(): string {
  const override = (globalThis as { __PRAMAN_NOTIFICATION_HUB_URL__?: string }).__PRAMAN_NOTIFICATION_HUB_URL__;
  if (override) return override;

  const hubPath = '/hubs/notifications';
  const apiBase = environment.apiBaseUrl;

  if (apiBase.startsWith('/')) {
    return hubPath;
  }

  try {
    const url = new URL(apiBase);
    return `${url.protocol}//${url.host}${hubPath}`;
  } catch {
    return hubPath;
  }
}
