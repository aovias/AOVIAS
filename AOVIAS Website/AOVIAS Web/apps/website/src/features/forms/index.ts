/**
 * AOVIAS Form Engine — configuration-driven enterprise forms.
 *
 * Import: `import { FormEngine, contactFormConfig, … } from '@/features/forms'`
 */

export {
  FormEngine,
  type FormConfig,
  type FormEngineProps,
  type FormFieldConfig,
  type FormFieldOption,
  type FormFieldType,
  type FormSectionConfig,
  type FormStatus,
  type FormSubmitFailure,
  type FormSubmitHandler,
  type FormSubmitResult,
  type FormSubmitSuccess,
  type FormSuccessConfig,
} from './engine';

export {
  createMockSubmitHandler,
  toFormSubmitFailure,
  zodResolver,
} from './adapters';

export { Field, FormFieldControl, fieldA11yProps, type FieldProps } from './fields';

export {
  useFormEngine,
  type UseFormEngineOptions,
  type UseFormEngineReturn,
} from './hooks';

export {
  billingFormConfig,
  billingFormSchema,
  checkoutConfirmFormConfig,
  checkoutConfirmFormSchema,
  contactFormConfig,
  contactFormSchema,
  demoRequestFormConfig,
  demoRequestFormSchema,
  enterpriseInquiryFormConfig,
  enterpriseInquiryFormSchema,
  forgotPasswordFormConfig,
  forgotPasswordFormSchema,
  newsletterFormConfig,
  newsletterFormSchema,
  partnerFormConfig,
  partnerFormSchema,
  resendVerificationFormConfig,
  resendVerificationFormSchema,
  resetPasswordFormConfig,
  resetPasswordFormSchema,
  signInFormConfig,
  signInFormSchema,
  signUpFormConfig,
  signUpFormSchema,
  type BillingFormSchemaValues,
  type CheckoutConfirmFormValues,
  type ContactFormValues,
  type DemoRequestFormValues,
  type EnterpriseInquiryFormValues,
  type ForgotPasswordFormValues,
  type NewsletterFormValues,
  type PartnerFormValues,
  type ResendVerificationFormValues,
  type ResetPasswordFormValues,
  type SignInFormValues,
  type SignUpFormValues,
} from './schemas';

export {
  FormErrorBanner,
  FormSuccess,
  type FormErrorProps,
  type FormSuccessProps,
} from './states';

export {
  consentField,
  emailField,
  lengthString,
  optionalBooleanField,
  optionalEmailField,
  optionalLengthString,
  optionalPhoneField,
  optionalUrlField,
  passwordField,
  patternString,
  phoneField,
  requiredString,
  selectField,
  validationMessages,
  withCustom,
} from './validators';
