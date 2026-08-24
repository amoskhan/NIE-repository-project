import { mount } from "@vue/test-utils";
import { describe, expect, it } from "vitest";
import AccessControlRolesPanel from "@/components/admin/access-control/AccessControlRolesPanel.vue";
import type { AccessFunction, Role } from "@/types";

const accessFunctions: AccessFunction[] = [
  {
    id: "0198fc41-bdf2-7a85-8475-7a2412147e01",
    code: "screen.access-control.view",
    name: "View access control",
    description: "Open the unified access-control administration screen.",
    module: "Access Control",
    type: 1,
    resourceName: "AccessControl",
    route: "/staff/access-control",
    isActive: true,
    isSystemFunction: true,
    displayOrder: 10,
  },
  {
    id: "0198fc41-bdf2-7a85-8475-7a2412147e02",
    code: "api.access-control.read",
    name: "Read access control",
    description:
      "Read roles, assignments, application scopes, and access functions.",
    module: "Access Control",
    type: 2,
    resourceName: "AccessControlController.GetOverview",
    httpMethod: "GET",
    isActive: true,
    isSystemFunction: true,
    displayOrder: 20,
  },
];

const roles: Role[] = [
  {
    id: "0198fc41-bdf2-7a85-8475-7a2412147d01",
    code: "SYSTEM_ADMIN",
    name: "System Administrator",
    description: "Full platform administration.",
    isActive: true,
    isSystemRole: true,
    displayOrder: 10,
    assignedUserCount: 2,
    accessFunctionIds: [accessFunctions[0].id],
    accessFunctions: [accessFunctions[0]],
  },
  {
    id: "0198fc41-bdf2-7a85-8475-7a2412147d02",
    code: "PROCUREMENT_OFFICER",
    name: "Procurement Officer",
    description: "Manages procurement operations.",
    isActive: true,
    isSystemRole: false,
    displayOrder: 20,
    assignedUserCount: 5,
    accessFunctionIds: [],
    accessFunctions: [],
  },
];

describe("AccessControlRolesPanel", () => {
  it("uses a vertical role selector and groups access functions with type and help metadata", async () => {
    const wrapper = mount(AccessControlRolesPanel, {
      props: { roles, accessFunctions },
    });

    const roleTabs = wrapper.get('[role="tablist"]');
    expect(roleTabs.attributes("aria-orientation")).toBe("vertical");
    expect(wrapper.findAll('[role="tab"]')).toHaveLength(2);
    expect(wrapper.text()).toContain("Access Control");

    const grants = wrapper.findAll(".access-function-grant");
    expect(grants).toHaveLength(2);
    expect(grants[0].get(".access-function-type").text()).toBe("Screen");
    expect(grants[1].get(".access-function-type").text()).toBe("API");
    expect(grants[0].get('input[type="checkbox"]').classes()).toContain(
      "rounded-full",
    );

    const help = grants[0].get(
      'button[aria-label="About View access control"]',
    );
    await help.trigger("click");
    expect(grants[0].text()).toContain(
      "Open the unified access-control administration screen.",
    );
  });

  it("edits role metadata in a popup and saves a multi-function grant set", async () => {
    const wrapper = mount(AccessControlRolesPanel, {
      attachTo: document.body,
      props: { roles, accessFunctions },
    });

    await wrapper
      .get('[aria-label="Edit System Administrator"]')
      .trigger("click");
    expect(document.body.textContent).toContain("Edit role");
    expect(document.body.textContent).toContain("Role name");
    expect(document.body.textContent).toContain("Description");

    const apiGrant = wrapper
      .findAll(".access-function-grant")
      .find((item) => item.text().includes("Read access control"));
    await apiGrant!.get('input[type="checkbox"]').setValue(true);
    await wrapper.get('[data-testid="save-role-access"]').trigger("click");

    expect(wrapper.emitted("save-role-access")?.at(-1)).toEqual([
      {
        roleId: roles[0].id,
        accessFunctionIds: [accessFunctions[0].id, accessFunctions[1].id],
      },
    ]);

    wrapper.unmount();
  });
});
