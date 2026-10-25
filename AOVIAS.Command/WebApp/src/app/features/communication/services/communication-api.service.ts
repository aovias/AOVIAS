import { Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';
import { BaseApiService } from '../../../shared/services/base-api.service';

export interface NotificationItem {
  id: string;
  title: string;
  body: string;
  moduleCode: string;
  isRead: boolean;
  createdAt: string;
}

export interface PagedNotifications {
  items: NotificationItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PreferenceItem {
  id: string;
  moduleCode?: string;
  channel: string;
  frequency: string;
  enabled: boolean;
  escalationOverride: boolean;
  noMarketing: boolean;
  quietHoursStart?: string;
  quietHoursEnd?: string;
}

export interface TemplateItem {
  id: string;
  code: string;
  name: string;
  channel: string;
  moduleCode: string;
  status: string;
  currentVersion: number;
}

export interface TimelineItem {
  id: string;
  source: string;
  eventType: string;
  summary: string;
  occurredAt: string;
}

export interface DeliveryDashboard {
  pendingMessages: number;
  deliveredToday: number;
  failedToday: number;
  unreadInApp: number;
  /** @deprecated Use pendingMessages */
  pendingOutbound?: number;
  /** @deprecated Use failedToday */
  failedDeliveries?: number;
}

export interface ReminderItem {
  id: string;
  name: string;
  moduleCode: string;
  cronExpression: string;
  templateCode: string;
  isActive: boolean;
}

export interface CampaignItem {
  id: string;
  name: string;
  moduleCode: string;
  status: string;
  scheduledAt?: string;
}

export interface AvailabilityProfile {
  id: string;
  status: string;
  timezone: string;
  workingHours: { dayOfWeek: number; startTime: string; endTime: string }[];
  vacations: { startDate: string; endDate: string }[];
}

export interface ChannelItem {
  channel: string;
  enabled: boolean;
  implemented: boolean;
  description: string;
}

export interface ProviderItem {
  id: string;
  channel: string;
  providerType: string;
  enabled: boolean;
}

export interface SubscriptionItem {
  id: string;
  sourceEventType: string;
  moduleCode: string;
  templateCode: string;
  channels: string;
  enabled: boolean;
}

export interface DeliveryHistoryItem {
  id: string;
  outboundMessageId: string;
  recipientPersonId?: string;
  channel: string;
  status: string;
  retryCount: number;
  sentAt?: string;
  providerType?: string;
  failureReason?: string;
  correlationId?: string;
  eventType?: string;
}

export interface QueueStatus {
  pending: number;
  processing: number;
  failed: number;
  retrying: number;
  completed: number;
}

@Injectable({ providedIn: 'root' })
export class CommunicationApiService extends BaseApiService {
  listNotifications(page = 1, pageSize = 20): Observable<PagedNotifications> {
    return this.get<PagedNotifications | NotificationItem[]>('/notifications', { page, pageSize }).pipe(
      map((data) => {
        if (Array.isArray(data)) {
          return { items: data, totalCount: data.length, page, pageSize };
        }
        return data;
      }),
    );
  }

  getUnreadCount(): Observable<number> {
    return this.get<number>('/notifications/unread-count');
  }

  markRead(id: string): Observable<unknown> {
    return this.post<unknown>(`/notifications/${id}/read`, {});
  }

  markAllRead(): Observable<unknown> {
    return this.post<unknown>('/notifications/read-all', {});
  }

  listPreferences(): Observable<PreferenceItem[]> {
    return this.get<PreferenceItem[]>('/preferences');
  }

  updatePreferences(body: Record<string, unknown>): Observable<PreferenceItem> {
    return this.put<PreferenceItem>('/preferences', body);
  }

  listTemplates(): Observable<TemplateItem[]> {
    return this.get<TemplateItem[]>('/templates');
  }

  upsertTemplate(body: Record<string, unknown>): Observable<TemplateItem> {
    return this.post<TemplateItem>('/templates', body);
  }

  getHistory(take = 100): Observable<TimelineItem[]> {
    return this.get<TimelineItem[]>('/communication/history', { take });
  }

  getDashboard(): Observable<DeliveryDashboard> {
    return this.get<DeliveryDashboard>('/communication/dashboard').pipe(
      map((d) => ({
        ...d,
        pendingOutbound: d.pendingMessages,
        failedDeliveries: d.failedToday,
      })),
    );
  }

  listChannels(): Observable<ChannelItem[]> {
    return this.get<ChannelItem[]>('/communication/channels');
  }

  listProviders(): Observable<ProviderItem[]> {
    return this.get<ProviderItem[]>('/communication/providers');
  }

  listSubscriptions(): Observable<SubscriptionItem[]> {
    return this.get<SubscriptionItem[]>('/communication/subscriptions');
  }

  listDeliveryHistory(take = 100): Observable<DeliveryHistoryItem[]> {
    return this.get<DeliveryHistoryItem[]>('/communication/delivery-history', { take });
  }

  getQueueStatus(): Observable<QueueStatus> {
    return this.get<QueueStatus>('/communication/queues');
  }

  listReminders(): Observable<ReminderItem[]> {
    return this.get<ReminderItem[]>('/reminders');
  }

  listCampaigns(): Observable<CampaignItem[]> {
    return this.get<CampaignItem[]>('/campaigns');
  }

  getAvailability(): Observable<AvailabilityProfile | null> {
    return this.get<AvailabilityProfile | null>('/availability');
  }
}
