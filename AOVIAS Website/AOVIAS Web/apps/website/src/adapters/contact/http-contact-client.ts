import type {
  ContactClient,
  ContactSubmission,
  ContactSubmissionResult,
} from '@aovias/sdk/contact';

type ContactApiResponse = {
  ok: boolean;
  submissionId?: string;
  message?: string;
  followUp?: string;
  retryable?: boolean;
};

async function postContact(
  input: ContactSubmission,
): Promise<ContactSubmissionResult> {
  try {
    const response = await fetch('/api/contact', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        intent: input.intent,
        payload: input.payload,
      }),
    });

    const data = (await response.json().catch(() => ({}))) as ContactApiResponse;

    if (!response.ok || !data.ok) {
      return {
        ok: false,
        message:
          data.message ||
          'Something went wrong while sending your message. Please try again.',
        retryable: data.retryable ?? response.status >= 500,
      };
    }

    return {
      ok: true,
      ...(data.submissionId ? { submissionId: data.submissionId } : {}),
      ...(data.message ? { message: data.message } : {}),
      ...(data.followUp ? { followUp: data.followUp } : {}),
    };
  } catch {
    return {
      ok: false,
      message:
        'Network error. Check your connection and try again, or email admin@aovias.com.',
      retryable: true,
    };
  }
}

/**
 * Browser Contact SDK — posts to same-origin /api/contact (server sends email).
 */
export function createHttpContactClient(): ContactClient {
  return {
    submitContact: postContact,
    submitDemoRequest: postContact,
    submitEnterpriseInquiry: postContact,
    submitPartnerInquiry: postContact,
    submitNewsletter: postContact,
    getClientInfo() {
      return { name: 'contact', mode: 'http', version: '1.0.0' };
    },
  };
}
