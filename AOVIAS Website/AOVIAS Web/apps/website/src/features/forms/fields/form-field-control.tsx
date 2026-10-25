'use client';

import {
  Controller,
  useFormContext,
  type FieldValues,
  type Path,
} from 'react-hook-form';

import {
  Checkbox,
  Input,
  Radio,
  Select,
  Stack,
  Switch,
  Text,
  Textarea,
} from '@/components/primitives';
import type { FormFieldConfig } from '@/features/forms/engine/types';
import { Field, fieldA11yProps } from '@/features/forms/fields/field';

type FormFieldControlProps = {
  field: FormFieldConfig;
  disabled?: boolean;
};

function fieldShellProps(field: FormFieldConfig, error?: string) {
  return {
    id: field.name,
    ...(field.label !== undefined ? { label: field.label } : {}),
    ...(field.description !== undefined
      ? { description: field.description }
      : {}),
    ...(error !== undefined ? { error } : {}),
    ...(field.required ? { required: true } : {}),
    ...(field.colSpan === 2 ? { className: 'md:col-span-2' } : {}),
  };
}

/**
 * Maps a FieldConfig + react-hook-form context onto a primitive control.
 */
export function FormFieldControl({ field, disabled }: FormFieldControlProps) {
  const {
    register,
    control,
    formState: { errors },
  } = useFormContext();

  const name = field.name as Path<FieldValues>;
  const errorMessage = errors[field.name]?.message;
  const error =
    typeof errorMessage === 'string' ? errorMessage : undefined;
  const isDisabled = Boolean(disabled || field.disabled);
  const a11y = fieldA11yProps(
    field.name,
    error,
    field.description,
    field.required,
  );

  if (field.type === 'hidden') {
    return <input type="hidden" {...register(name)} />;
  }

  if (field.type === 'honeypot') {
    return (
      <div
        className="absolute -left-[9999px] h-0 w-0 overflow-hidden opacity-0"
        aria-hidden="true"
      >
        <label htmlFor={field.name}>
          {field.label ?? 'Leave blank'}
          <input
            id={field.name}
            type="text"
            tabIndex={-1}
            autoComplete="off"
            {...register(name)}
          />
        </label>
      </div>
    );
  }

  if (field.type === 'textarea') {
    return (
      <Field {...fieldShellProps(field, error)}>
        <Textarea
          {...register(name)}
          {...a11y}
          {...(field.placeholder !== undefined
            ? { placeholder: field.placeholder }
            : {})}
          disabled={isDisabled}
          {...(field.readOnly ? { readOnly: true } : {})}
          {...(field.autoComplete !== undefined
            ? { autoComplete: field.autoComplete }
            : {})}
        />
      </Field>
    );
  }

  if (field.type === 'select') {
    return (
      <Field {...fieldShellProps(field, error)}>
        <Select
          {...register(name)}
          {...a11y}
          disabled={isDisabled}
          {...(field.autoComplete !== undefined
            ? { autoComplete: field.autoComplete }
            : {})}
        >
          <option value="">{field.placeholder ?? 'Select…'}</option>
          {(field.options ?? []).map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </Select>
      </Field>
    );
  }

  if (field.type === 'checkbox') {
    return (
      <Field {...fieldShellProps(field, error)} inline>
        <Checkbox {...register(name)} {...a11y} disabled={isDisabled} />
      </Field>
    );
  }

  if (field.type === 'switch') {
    return (
      <Controller
        name={name}
        control={control}
        render={({ field: rhfField }) => (
          <Field {...fieldShellProps(field, error)} inline>
            <Switch
              id={field.name}
              checked={Boolean(rhfField.value)}
              onCheckedChange={rhfField.onChange}
              disabled={isDisabled}
              {...(field.label !== undefined
                ? { 'aria-label': field.label }
                : {})}
            />
          </Field>
        )}
      />
    );
  }

  if (field.type === 'radio') {
    return (
      <Controller
        name={name}
        control={control}
        render={({ field: rhfField }) => (
          <fieldset
            className={
              field.colSpan === 2 ? 'md:col-span-2 w-full' : 'w-full'
            }
            {...(error ? { 'aria-invalid': true } : {})}
            {...(a11y['aria-describedby']
              ? { 'aria-describedby': a11y['aria-describedby'] }
              : {})}
          >
            {field.label ? (
              <legend className="text-foreground mb-2 text-small font-medium">
                {field.label}
                {field.required ? (
                  <span className="text-error ms-0.5" aria-hidden="true">
                    *
                  </span>
                ) : null}
              </legend>
            ) : null}
            <Stack gap={2}>
              {(field.options ?? []).map((option) => {
                const optionId = `${field.name}-${option.value}`;
                return (
                  <label
                    key={option.value}
                    htmlFor={optionId}
                    className="flex items-center gap-2"
                  >
                    <Radio
                      id={optionId}
                      name={field.name}
                      value={option.value}
                      checked={rhfField.value === option.value}
                      onChange={() => rhfField.onChange(option.value)}
                      disabled={isDisabled}
                    />
                    <Text as="span" size="small">
                      {option.label}
                    </Text>
                  </label>
                );
              })}
            </Stack>
            {field.description ? (
              <p
                id={`${field.name}-description`}
                className="text-muted-foreground mt-2 text-caption"
              >
                {field.description}
              </p>
            ) : null}
            {error ? (
              <p
                id={`${field.name}-error`}
                role="alert"
                className="text-error mt-2 text-caption"
              >
                {error}
              </p>
            ) : null}
          </fieldset>
        )}
      />
    );
  }

  const inputType =
    field.type === 'email' ||
    field.type === 'tel' ||
    field.type === 'password'
      ? field.type
      : 'text';

  return (
    <Field {...fieldShellProps(field, error)}>
      <Input
        type={inputType}
        {...register(name)}
        {...a11y}
        {...(field.placeholder !== undefined
          ? { placeholder: field.placeholder }
          : {})}
        disabled={isDisabled}
        {...(field.readOnly ? { readOnly: true } : {})}
        {...(field.autoComplete !== undefined
          ? { autoComplete: field.autoComplete }
          : {})}
      />
    </Field>
  );
}
