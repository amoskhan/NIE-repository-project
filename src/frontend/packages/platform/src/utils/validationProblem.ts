import { z } from "zod";

const validationProblemSchema = z
  .object({
    detail: z.string().optional(),
    errors: z.record(z.array(z.string())).optional(),
  })
  .passthrough();

const responseEnvelopeSchema = z
  .object({
    response: z
      .object({
        data: z.unknown(),
      })
      .passthrough(),
  })
  .passthrough();

export type ValidationFieldErrors = Record<string, string>;

/**
 * Extracts the first message for each field from an RFC 7807 validation
 * response or from an HTTP-client error whose response contains that payload.
 */
export function getValidationFieldErrors(
  input: unknown,
): ValidationFieldErrors {
  const envelope = responseEnvelopeSchema.safeParse(input);
  const candidate = envelope.success ? envelope.data.response.data : input;
  const problem = validationProblemSchema.safeParse(candidate);
  if (!problem.success || !problem.data.errors) return {};

  return Object.fromEntries(
    Object.entries(problem.data.errors)
      .filter(([, messages]) => messages.length > 0)
      .map(([field, messages]) => [field, messages[0]]),
  );
}
