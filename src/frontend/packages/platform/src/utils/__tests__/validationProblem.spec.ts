import { describe, expect, it } from "vitest";
import { getValidationFieldErrors } from "../validationProblem";

describe("getValidationFieldErrors", () => {
  it("keeps only the first message for each field", () => {
    const problem = {
      title: "One or more validation errors occurred.",
      status: 400,
      errors: {
        email: ["Email is required.", "Email must be a valid address."],
        name: ["Name is required."],
      },
    };

    expect(getValidationFieldErrors(problem)).toEqual({
      email: "Email is required.",
      name: "Name is required.",
    });
  });

  it("unwraps an HTTP-client error whose response carries the problem", () => {
    const axiosLikeError = {
      message: "Request failed with status code 400",
      response: {
        status: 400,
        data: {
          detail: "Validation failed",
          errors: { "address.postalCode": ["Postal code is invalid."] },
        },
      },
    };

    expect(getValidationFieldErrors(axiosLikeError)).toEqual({
      "address.postalCode": "Postal code is invalid.",
    });
  });

  it("drops fields whose message list is empty", () => {
    const problem = { errors: { email: [], name: ["Name is required."] } };

    expect(getValidationFieldErrors(problem)).toEqual({
      name: "Name is required.",
    });
  });

  it("returns an empty map when there is nothing to extract", () => {
    expect(getValidationFieldErrors(undefined)).toEqual({});
    expect(getValidationFieldErrors(null)).toEqual({});
    expect(getValidationFieldErrors("boom")).toEqual({});
    expect(getValidationFieldErrors({ detail: "No field errors" })).toEqual({});
    expect(getValidationFieldErrors({ response: { data: 42 } })).toEqual({});
  });

  it("ignores an errors payload that is not a map of string arrays", () => {
    expect(getValidationFieldErrors({ errors: { email: "not-an-array" } })).toEqual(
      {},
    );
    expect(getValidationFieldErrors({ errors: [] })).toEqual({});
  });
});
