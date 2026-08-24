import { afterEach, describe, expect, it } from "vitest";
import { useConfirm } from "../useConfirm";

afterEach(() => {
  useConfirm().handleCancel();
});

describe("useConfirm", () => {
  it("normalises a bare message into confirm options", () => {
    const { state, confirm } = useConfirm();

    void confirm("Delete this purchase order?");

    expect(state.value.options).toEqual({
      message: "Delete this purchase order?",
    });
  });

  it("keeps rich options as supplied", () => {
    const { state, confirm } = useConfirm();

    void confirm({
      title: "Delete order",
      message: "This cannot be undone.",
      confirmText: "Delete",
      cancelText: "Keep",
      variant: "danger",
    });

    expect(state.value.options).toEqual({
      title: "Delete order",
      message: "This cannot be undone.",
      confirmText: "Delete",
      cancelText: "Keep",
      variant: "danger",
    });
  });

  it("stays pending until the dialog is answered", async () => {
    const { confirm, handleConfirm } = useConfirm();
    let settled = false;

    const pending = confirm("Continue?").then((answer) => {
      settled = true;
      return answer;
    });

    expect(settled).toBe(false);

    handleConfirm();

    await expect(pending).resolves.toBe(true);
  });

  it("resolves false when the dialog is cancelled", async () => {
    const { confirm, handleCancel } = useConfirm();

    const pending = confirm("Continue?");
    handleCancel();

    await expect(pending).resolves.toBe(false);
  });

  it("closes the dialog after either answer", async () => {
    const { state, confirm, handleConfirm, handleCancel } = useConfirm();

    const confirmed = confirm("Continue?");
    handleConfirm();
    await confirmed;
    expect(state.value.options).toBeNull();

    const cancelled = confirm("Continue?");
    handleCancel();
    await cancelled;
    expect(state.value.options).toBeNull();
  });

  it("ignores a second answer for an already-closed dialog", async () => {
    const { confirm, handleConfirm, handleCancel } = useConfirm();

    const pending = confirm("Continue?");
    handleConfirm();

    expect(() => {
      handleCancel();
    }).not.toThrow();
    await expect(pending).resolves.toBe(true);
  });

  it("shares the dialog state across call sites", () => {
    const opener = useConfirm();
    const dialog = useConfirm();

    void opener.confirm("Continue?");

    expect(dialog.state.value.options?.message).toBe("Continue?");
  });
});
