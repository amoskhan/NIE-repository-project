import { flushPromises, mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import AccessAssignmentModal from "@/components/admin/access-control/AccessAssignmentModal.vue";
import type { ApplicationSummary, Role, StaffDetails } from "@/types";

const roles: Role[] = [
  {
    id: "0198fc41-bdf2-7a85-8475-7a2412147d01",
    code: "SYSTEM_ADMIN",
    name: "System Administrator",
    description: "Full administration",
    isActive: true,
    displayOrder: 10,
    accessFunctionIds: [],
  },
  {
    id: "0198fc41-bdf2-7a85-8475-7a2412147d02",
    code: "PROCUREMENT_OFFICER",
    name: "Procurement Officer",
    description: "Procurement operations",
    isActive: true,
    displayOrder: 20,
    accessFunctionIds: [],
  },
];

const applications: ApplicationSummary[] = [
  {
    id: "0198fc41-bdf2-7a85-8475-7a2412147a01",
    name: "NIE Template",
    projectKey: "nie-template",
    isActive: true,
  },
  {
    id: "0198fc41-bdf2-7a85-8475-7a2412147a02",
    name: "Procurement Sample",
    projectKey: "procurement",
    isActive: true,
  },
];

const staff: StaffDetails = {
  workerId: "1001",
  userId: "alex",
  name: "Alex Tan",
  email: "alex@nie.edu.sg",
  department: "ICT",
  departmentDescription: "Information Technology",
  designation: "Manager",
  title: "Mr",
};

describe("AccessAssignmentModal", () => {
  it("defers required-field messages until the first submit attempt", async () => {
    const wrapper = mount(AccessAssignmentModal, {
      attachTo: document.body,
      props: {
        modelValue: true,
        roles,
        applications,
      },
    });

    expect(document.body.textContent).not.toContain(
      "Resolve a staff member first",
    );
    expect(document.body.textContent).not.toContain("Select at least one role");

    await wrapper.vm.submit();
    await flushPromises();

    expect(document.body.textContent).toContain("Resolve a staff member first");
    expect(document.body.textContent).toContain("Select at least one role");
    expect(
      document.body.querySelector(
        '[data-testid="assignment-validation-summary"]',
      )?.textContent,
    ).toContain("Resolve a staff member first");

    wrapper.unmount();
  });

  it("supports multiple roles and multiple applications for application-level access", async () => {
    const wrapper = mount(AccessAssignmentModal, {
      attachTo: document.body,
      props: {
        modelValue: true,
        roles,
        applications,
        resolvedStaff: staff,
      },
    });

    const applicationScope = document.body.querySelector<HTMLInputElement>(
      'input[value="application"]',
    );
    applicationScope!.click();
    await wrapper.vm.$nextTick();

    const roleInputs = Array.from(
      document.body.querySelectorAll<HTMLInputElement>(
        '[data-testid="assignment-role"]',
      ),
    );
    roleInputs.forEach((input) => input.click());

    const applicationInputs = Array.from(
      document.body.querySelectorAll<HTMLInputElement>(
        '[data-testid="assignment-application"]',
      ),
    );
    applicationInputs.forEach((input) => input.click());
    await wrapper.vm.$nextTick();
    expect(roleInputs.every((input) => input.checked)).toBe(true);
    expect(applicationInputs.every((input) => input.checked)).toBe(true);

    await wrapper.vm.submit();
    await flushPromises();

    expect(document.body.textContent).not.toContain(
      "Resolve a staff member first",
    );
    expect(document.body.textContent).not.toContain("Select at least one role");
    expect(document.body.textContent).not.toContain(
      "Select at least one application",
    );
    expect(
      document.body.querySelector(
        '[data-testid="assignment-validation-summary"]',
      )?.textContent,
    ).toBeUndefined();

    expect(wrapper.emitted("assign")?.at(-1)).toEqual([
      {
        scope: "application",
        userId: "alex",
        roleIds: roles.map((role) => role.id),
        applicationIds: applications.map((application) => application.id),
      },
    ]);

    wrapper.unmount();
  });
});
