// EXAMPLE UNIT TEST — a composable, tested without mounting a component.
//
// Composables that only use ref/computed (no lifecycle hooks) can be called straight
// from a test. That is a good reason to keep logic in composables rather than in a
// component's <script setup>.

import { describe, expect, it } from "vitest";
import { z } from "zod";
import { useFormValidation } from "./useFormValidation";

const schema = z.object({
  name: z.string().min(1, "Name is required"),
  email: z.string().email("Enter a valid email"),
});

describe("useFormValidation", () => {
  it("returns the parsed data and no errors when the input is valid", () => {
    const { validate, errors } = useFormValidation(schema);

    const parsed = validate({ name: "Jane", email: "jane@example.edu" });

    expect(parsed).toEqual({ name: "Jane", email: "jane@example.edu" });
    expect(errors.value).toEqual({});
  });

  it("returns null and exposes one message per invalid field", () => {
    const { validate, errors, hasError } = useFormValidation(schema);

    const parsed = validate({ name: "", email: "not-an-email" });

    expect(parsed).toBeNull();
    expect(errors.value.name).toBe("Name is required");
    expect(errors.value.email).toBe("Enter a valid email");
    expect(hasError("name")).toBe(true);
    expect(hasError("department")).toBe(false);
  });

  it("clears stale errors once the input becomes valid", () => {
    const { validate, errors, clearErrors } = useFormValidation(schema);

    validate({ name: "", email: "" });
    expect(Object.keys(errors.value).length).toBeGreaterThan(0);

    // A successful validate() must reset the error state, otherwise the form keeps
    // showing messages the user has already fixed.
    validate({ name: "Jane", email: "jane@example.edu" });
    expect(errors.value).toEqual({});

    validate({ name: "", email: "" });
    clearErrors();
    expect(errors.value).toEqual({});
  });
});
