import type { FieldValues } from 'react-hook-form';

import type { FormSubmitHandler, FormSubmitResult } from '@/features/forms';
import { getContactClient } from '@/registries/sdk-registry';
import { getTelemetryClient } from '@/telemetry';

function toFormResult(result: {
  ok: boolean;
  message?: string;
  followUp?: string;
  retryable?: boolean;
}): FormSubmitResult {
  if (result.ok) {
    return {
      ok: true,
      ...(result.message ? { message: result.message } : {}),
      ...(result.followUp ? { followUp: result.followUp } : {}),
    };
  }
  return {
    ok: false,
    message: result.message ?? 'Something went wrong. Please try again.',
    ...(result.retryable !== undefined ? { retryable: result.retryable } : {}),
  };
}

function trackContactSuccess(
  form: string,
  conversionName:
    | 'conversion.contact_submission'
    | 'conversion.demo_request'
    | 'conversion.enterprise_inquiry'
    | 'conversion.partner_inquiry',
  domainName:
    | 'contact.submitted'
    | 'contact.demo_requested'
    | 'contact.enterprise_inquiry'
    | 'contact.partner_inquiry',
): void {
  const telemetry = getTelemetryClient();
  const attributes = { 'contact.form': form };
  telemetry.track(domainName, attributes);
  telemetry.trackConversion({ name: conversionName, attributes });
}

/** Bridge ContactClient → FormEngine (+ telemetry contracts; inactive until R2). */
export function createContactSubmitHandler<
  TValues extends FieldValues,
>(): FormSubmitHandler<TValues> {
  return async (values) => {
    const result = await getContactClient().submitContact({
      intent: 'contact',
      payload: values as Readonly<Record<string, unknown>>,
    });
    if (result.ok) {
      trackContactSuccess(
        'contact',
        'conversion.contact_submission',
        'contact.submitted',
      );
    }
    return toFormResult(result);
  };
}

export function createDemoRequestSubmitHandler<
  TValues extends FieldValues,
>(): FormSubmitHandler<TValues> {
  return async (values) => {
    const result = await getContactClient().submitDemoRequest({
      intent: 'demo',
      payload: values as Readonly<Record<string, unknown>>,
    });
    if (result.ok) {
      trackContactSuccess(
        'demo',
        'conversion.demo_request',
        'contact.demo_requested',
      );
    }
    return toFormResult(result);
  };
}

export function createEnterpriseInquirySubmitHandler<
  TValues extends FieldValues,
>(): FormSubmitHandler<TValues> {
  return async (values) => {
    const result = await getContactClient().submitEnterpriseInquiry({
      intent: 'enterprise',
      payload: values as Readonly<Record<string, unknown>>,
    });
    if (result.ok) {
      trackContactSuccess(
        'enterprise',
        'conversion.enterprise_inquiry',
        'contact.enterprise_inquiry',
      );
    }
    return toFormResult(result);
  };
}

export function createPartnerInquirySubmitHandler<
  TValues extends FieldValues,
>(): FormSubmitHandler<TValues> {
  return async (values) => {
    const result = await getContactClient().submitPartnerInquiry({
      intent: 'partner',
      payload: values as Readonly<Record<string, unknown>>,
    });
    if (result.ok) {
      trackContactSuccess(
        'partner',
        'conversion.partner_inquiry',
        'contact.partner_inquiry',
      );
    }
    return toFormResult(result);
  };
}
