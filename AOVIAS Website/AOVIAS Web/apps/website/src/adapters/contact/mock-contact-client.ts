import type { ContactClient, ContactSubmissionResult } from '@aovias/sdk/contact';

import { mockDelay } from '@/mocks/delay';

const MOCK_FOLLOW_UP =
  'This environment recorded the inquiry. Configure EMAIL_PROVIDER to deliver live mail to admin@aovias.com.';

function success(message: string): ContactSubmissionResult {
  return {
    ok: true,
    submissionId: `CNT-${Date.now().toString(36).toUpperCase()}`,
    message,
    followUp: MOCK_FOLLOW_UP,
  };
}

/**
 * Mock Contact SDK — local evaluation only (tests / offline).
 * Production uses createHttpContactClient → /api/contact.
 */
export function createMockContactClient(): ContactClient {
  return {
    async submitContact() {
      await mockDelay();
      return success(
        'Thank you for contacting AOVIAS. Our team has received your enquiry and will respond shortly.',
      );
    },

    async submitDemoRequest() {
      await mockDelay();
      return success('Demo request recorded.');
    },

    async submitEnterpriseInquiry() {
      await mockDelay();
      return success('Enterprise inquiry recorded.');
    },

    async submitPartnerInquiry() {
      await mockDelay();
      return success('Partner inquiry recorded.');
    },

    async submitNewsletter() {
      await mockDelay();
      return success('Subscription interest recorded.');
    },

    getClientInfo() {
      return { name: 'contact', mode: 'mock', version: '0.1.0' };
    },
  };
}
