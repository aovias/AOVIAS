'use client';

import { FormProvider } from 'react-hook-form';
import type { FieldValues } from 'react-hook-form';

import { Button, Heading, Paragraph, Stack, Text } from '@/components/primitives';
import type {
  FormConfig,
  FormSubmitHandler,
} from '@/features/forms/engine/types';
import { FormFieldControl } from '@/features/forms/fields/form-field-control';
import { useFormEngine } from '@/features/forms/hooks/use-form-engine';
import { FormErrorBanner } from '@/features/forms/states/form-error';
import { FormSuccess } from '@/features/forms/states/form-success';
import { cn } from '@/lib/utils';

export type FormEngineProps<TValues extends FieldValues> = {
  config: FormConfig<TValues>;
  onSubmit: FormSubmitHandler<TValues>;
  className?: string;
  /** Disable all fields (e.g. read-only preview). */
  disabled?: boolean;
};

/**
 * Form Engine — renders a FormConfig with shared validation, status, and fields.
 */
export function FormEngine<TValues extends FieldValues>({
  config,
  onSubmit,
  className,
  disabled = false,
}: FormEngineProps<TValues>) {
  const engine = useFormEngine<TValues>({
    schema: config.schema,
    onSubmit,
    ...(config.defaultValues !== undefined
      ? { defaultValues: config.defaultValues }
      : {}),
  });

  if (engine.status === 'success') {
    return (
      <FormSuccess
        {...(config.success?.title !== undefined
          ? { title: config.success.title }
          : {})}
        {...(engine.success?.message !== undefined
          ? { message: engine.success.message }
          : config.success?.message !== undefined
            ? { message: config.success.message }
            : {})}
        {...(engine.success?.followUp !== undefined
          ? { followUp: engine.success.followUp }
          : config.success?.followUp !== undefined
            ? { followUp: config.success.followUp }
            : {})}
        showIllustration={config.success?.showIllustration ?? true}
        onReset={engine.resetStatus}
      />
    );
  }

  const sections = config.sections ?? [];
  const columns = config.columns ?? 1;
  const isBusy = engine.isSubmitting || disabled;

  const renderFields = (sectionId?: string) =>
    config.fields
      .filter((field) =>
        sectionId ? field.section === sectionId : !field.section,
      )
      .map((field) => (
        <FormFieldControl
          key={field.name}
          field={field}
          {...(isBusy ? { disabled: true } : {})}
        />
      ));

  return (
    <FormProvider {...engine.form}>
        <form
        id={config.id}
        noValidate
        aria-label={config.ariaLabel}
        aria-busy={isBusy || undefined}
        onSubmit={(event) => {
          if (isBusy) {
            event.preventDefault();
            return;
          }
          void engine.handleSubmit(event);
        }}
        className={cn('relative w-full', className)}
      >
        <Stack gap={6}>
          {engine.submitError ? (
            <FormErrorBanner
              message={engine.submitError}
              onRetry={engine.retry}
            />
          ) : null}

          {sections.length > 0
            ? sections.map((section) => (
                <Stack key={section.id} gap={3} as="fieldset">
                  {section.title ? (
                    <legend className="float-left mb-2 w-full">
                      <Heading level={2} size="h4">
                        {section.title}
                      </Heading>
                      {section.description ? (
                        <Paragraph tone="muted" className="mt-1">
                          {section.description}
                        </Paragraph>
                      ) : null}
                    </legend>
                  ) : section.description ? (
                    <legend className="sr-only">{section.description}</legend>
                  ) : null}
                  <div
                    className={cn(
                      'grid gap-3 sm:gap-4',
                      columns === 2 && 'md:grid-cols-2',
                    )}
                  >
                    {renderFields(section.id)}
                  </div>
                </Stack>
              ))
            : null}

          {config.fields.some((field) => !field.section) ? (
            <div
              className={cn(
                'grid gap-4',
                columns === 2 && 'md:grid-cols-2',
              )}
            >
              {renderFields(undefined)}
            </div>
          ) : null}

          <div className="flex flex-wrap items-center gap-3">
            <Button
              type="submit"
              variant="primary"
              loading={engine.isSubmitting}
              disabled={isBusy}
            >
              {engine.isSubmitting
                ? (config.submittingLabel ?? 'Submitting…')
                : (config.submitLabel ?? 'Submit')}
            </Button>
            {engine.isSubmitting ? (
              <Text as="span" size="caption" tone="muted">
                Please wait…
              </Text>
            ) : null}
          </div>
        </Stack>
      </form>
    </FormProvider>
  );
}
