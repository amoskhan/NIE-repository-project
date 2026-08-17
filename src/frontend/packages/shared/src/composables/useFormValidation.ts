import { ref, type Ref } from "vue";
import { type ZodSchema, type ZodError } from "zod";

export type FieldErrors = Record<string, string | undefined>;

interface UseFormValidationReturn<T> {
  /** Reactive object holding per-field error messages (keyed by field name) */
  errors: Ref<FieldErrors>;
  /** Validate the form data. Returns the parsed data on success, or null on failure. */
  validate: (data: unknown) => T | null;
  /** Clear all errors */
  clearErrors: () => void;
  /** Check if a specific field has an error */
  hasError: (field: string) => boolean;
}

/**
 * Composable for Zod-based form validation with reactive error state.
 *
 * @example
 * ```vue
 * <script setup lang="ts">
 * import { useFormValidation } from '@apptemplate/shared/composables';
 * import { z } from 'zod';
 *
 * const schema = z.object({
 *   name: z.string().min(1, 'Name is required'),
 *   email: z.string().email('Invalid email'),
 * });
 *
 * const { errors, validate, hasError } = useFormValidation(schema);
 *
 * function onSubmit() {
 *   const parsed = validate({ name: form.name, email: form.email });
 *   if (parsed) {
 *     // submit parsed data
 *   }
 * }
 * </script>
 * ```
 */
export function useFormValidation<T>(
  schema: ZodSchema<T>,
): UseFormValidationReturn<T> {
  const errors = ref<FieldErrors>({}) as Ref<FieldErrors>;

  function validate(data: unknown): T | null {
    const result = schema.safeParse(data);
    if (result.success) {
      errors.value = {};
      return result.data;
    }

    const fieldErrors: FieldErrors = {};
    for (const issue of (result.error as ZodError).issues) {
      const key = issue.path.join(".");
      if (!fieldErrors[key]) {
        fieldErrors[key] = issue.message;
      }
    }
    errors.value = fieldErrors;
    return null;
  }

  function clearErrors() {
    errors.value = {};
  }

  function hasError(field: string): boolean {
    return !!errors.value[field];
  }

  return { errors, validate, clearErrors, hasError };
}
