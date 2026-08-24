import { mount } from "@vue/test-utils";
import { describe, expect, it, vi } from "vitest";
import NieFileUploadField from "../../file-upload/NieFileUploadField.vue";
import type { UploadedFileItem } from "../../file-upload/NieFileUploadField.vue";

function makeFile(name: string, size = 1024, lastModified = 1): File {
  const file = new File(["x"], name, { type: "text/plain", lastModified });
  Object.defineProperty(file, "size", { value: size });
  return file;
}

function asItem(file: File, id = "existing"): UploadedFileItem {
  return { id, name: file.name, size: file.size, type: file.type, file };
}

type UploadProps = InstanceType<typeof NieFileUploadField>["$props"];

function mountField(props: Partial<UploadProps> = {}) {
  return mount(NieFileUploadField, { props: props as UploadProps });
}

async function selectFiles(
  wrapper: ReturnType<typeof mountField>,
  files: File[],
) {
  const input = wrapper.get('input[type="file"]');
  Object.defineProperty(input.element, "files", {
    configurable: true,
    value: files,
  });
  await input.trigger("change");
}

function lastEmitted(
  wrapper: ReturnType<typeof mountField>,
): UploadedFileItem[] {
  const emitted = wrapper.emitted("update:modelValue") ?? [];
  return emitted[emitted.length - 1]?.[0] as UploadedFileItem[];
}

describe("NieFileUploadField rendering", () => {
  it("uses a default label and hint", () => {
    const wrapper = mountField();

    expect(wrapper.get("h3").text()).toBe("Files");
    expect(wrapper.text()).toContain(
      "Drop files here or browse from your device.",
    );
  });

  it("renders the optional description", () => {
    const bare = mountField();
    const described = mountField({ description: "PDF invoices only." });

    expect(bare.text()).not.toContain("PDF invoices only.");
    expect(described.text()).toContain("PDF invoices only.");
  });

  it("forwards accept and multiple to the native input", () => {
    const wrapper = mountField({ accept: ".pdf", multiple: false });
    const input = wrapper.get('input[type="file"]');

    expect(input.attributes("accept")).toBe(".pdf");
    expect(input.attributes("multiple")).toBeUndefined();
    expect(wrapper.text()).toContain("Accepted: .pdf");
  });

  it("shows no counter until a maximum is configured", () => {
    expect(mountField().find(".rounded-full").exists()).toBe(false);

    const limited = mountField({ maxFiles: 3 });
    expect(limited.get(".rounded-full").text()).toBe("0/3 files");
  });

  it("pluralises the remaining-slot hint", () => {
    expect(mountField({ maxFiles: 2 }).text()).toContain("2 slots left");
    expect(
      mountField({
        maxFiles: 2,
        modelValue: [asItem(makeFile("a.pdf"))],
      }).text(),
    ).toContain("1 slot left");
  });

  it("formats file sizes in bytes, kilobytes and megabytes", () => {
    const wrapper = mountField({
      modelValue: [
        asItem(makeFile("tiny.txt", 512), "a"),
        asItem(makeFile("small.txt", 2048), "b"),
        asItem(makeFile("big.txt", 3 * 1024 * 1024), "c"),
      ],
    });

    const text = wrapper.get("ul").text();
    expect(text).toContain("512 B");
    expect(text).toContain("2 KB");
    expect(text).toContain("3.0 MB");
  });

  it("merges a caller-supplied class", () => {
    expect(mountField({ class: "mt-4" }).classes()).toContain("mt-4");
  });
});

describe("NieFileUploadField selection", () => {
  it("maps chosen files into upload items", async () => {
    const wrapper = mountField();

    await selectFiles(wrapper, [makeFile("invoice.pdf", 2048)]);

    const files = lastEmitted(wrapper);
    expect(files).toHaveLength(1);
    expect(files[0]).toMatchObject({
      name: "invoice.pdf",
      size: 2048,
      type: "text/plain",
    });
    expect(files[0].id).toMatch(/^upload-/);
  });

  it("defaults an unknown MIME type to a binary stream", async () => {
    const wrapper = mountField();
    const file = new File(["x"], "unknown.bin", { type: "" });

    await selectFiles(wrapper, [file]);

    expect(lastEmitted(wrapper)[0].type).toBe("application/octet-stream");
  });

  it("appends to the existing selection when multiple is allowed", async () => {
    const existing = asItem(makeFile("first.pdf"), "first");
    const wrapper = mountField({ modelValue: [existing] });

    await selectFiles(wrapper, [makeFile("second.pdf", 2048, 2)]);

    expect(lastEmitted(wrapper).map((file) => file.name)).toEqual([
      "first.pdf",
      "second.pdf",
    ]);
  });

  it("replaces the selection when only one file is allowed", async () => {
    const wrapper = mountField({
      multiple: false,
      modelValue: [asItem(makeFile("first.pdf"), "first")],
    });

    await selectFiles(wrapper, [
      makeFile("second.pdf", 2048, 2),
      makeFile("third.pdf", 4096, 3),
    ]);

    expect(lastEmitted(wrapper).map((file) => file.name)).toEqual([
      "second.pdf",
    ]);
  });

  it("drops files that duplicate name, size and timestamp", async () => {
    const wrapper = mountField({
      modelValue: [asItem(makeFile("invoice.pdf", 1024, 7), "first")],
    });

    await selectFiles(wrapper, [makeFile("invoice.pdf", 1024, 7)]);

    expect(lastEmitted(wrapper)).toHaveLength(1);
  });

  it("keeps same-named files that differ in size", async () => {
    const wrapper = mountField({
      modelValue: [asItem(makeFile("invoice.pdf", 1024, 7), "first")],
    });

    await selectFiles(wrapper, [makeFile("invoice.pdf", 2048, 7)]);

    expect(lastEmitted(wrapper)).toHaveLength(2);
  });

  it("never exceeds the configured maximum", async () => {
    const wrapper = mountField({ maxFiles: 2 });

    await selectFiles(wrapper, [
      makeFile("a.pdf", 1, 1),
      makeFile("b.pdf", 2, 2),
      makeFile("c.pdf", 3, 3),
    ]);

    expect(lastEmitted(wrapper).map((file) => file.name)).toEqual([
      "a.pdf",
      "b.pdf",
    ]);
  });

  it("ignores an empty selection", async () => {
    const wrapper = mountField();

    await selectFiles(wrapper, []);

    expect(wrapper.emitted("update:modelValue")).toBeUndefined();
  });

  it("clears the native input so the same file can be picked again", async () => {
    const wrapper = mountField();

    await selectFiles(wrapper, [makeFile("invoice.pdf")]);

    expect(
      (wrapper.get('input[type="file"]').element as HTMLInputElement).value,
    ).toBe("");
  });
});

describe("NieFileUploadField drag and drop", () => {
  function dropEvent(files: File[]): DragEvent {
    const event = new Event("drop", { bubbles: true }) as DragEvent;
    Object.defineProperty(event, "dataTransfer", { value: { files } });
    return event;
  }

  it("accepts dropped files", async () => {
    const wrapper = mountField();

    wrapper.get(".border-dashed").element.dispatchEvent(
      dropEvent([makeFile("dropped.pdf", 2048)]),
    );
    await wrapper.vm.$nextTick();

    expect(lastEmitted(wrapper).map((file) => file.name)).toEqual([
      "dropped.pdf",
    ]);
  });

  it("ignores drops while disabled", async () => {
    const wrapper = mountField({ disabled: true });

    wrapper.get(".border-dashed").element.dispatchEvent(
      dropEvent([makeFile("dropped.pdf")]),
    );
    await wrapper.vm.$nextTick();

    expect(wrapper.emitted("update:modelValue")).toBeUndefined();
  });

  it("ignores a drop that carries no files", async () => {
    const wrapper = mountField();

    wrapper.get(".border-dashed").element.dispatchEvent(dropEvent([]));
    await wrapper.vm.$nextTick();

    expect(wrapper.emitted("update:modelValue")).toBeUndefined();
  });

  it("highlights the drop zone while a drag is over it", async () => {
    const wrapper = mountField();
    const zone = wrapper.get(".border-dashed");

    await zone.trigger("dragover");
    expect(zone.classes()).toContain("border-primary-500");

    await zone.trigger("dragleave");
    expect(zone.classes()).not.toContain("border-primary-500");
  });
});

describe("NieFileUploadField capacity", () => {
  it("opens the picker from the drop zone and the browse button", async () => {
    const wrapper = mountField();
    const click = vi.fn();
    (wrapper.get('input[type="file"]').element as HTMLInputElement).click =
      click;

    await wrapper.get(".border-dashed").trigger("click");
    await wrapper.get("button").trigger("click");

    expect(click).toHaveBeenCalledTimes(2);
  });

  it("refuses to open the picker once the maximum is reached", async () => {
    const wrapper = mountField({
      maxFiles: 1,
      modelValue: [asItem(makeFile("a.pdf"))],
    });
    const click = vi.fn();
    (wrapper.get('input[type="file"]').element as HTMLInputElement).click =
      click;

    await wrapper.get(".border-dashed").trigger("click");

    expect(click).not.toHaveBeenCalled();
    expect(wrapper.get("button").attributes("disabled")).toBeDefined();
    expect(wrapper.get(".border-dashed").classes()).toContain("opacity-70");
  });

  it("refuses to open the picker while disabled", async () => {
    const wrapper = mountField({ disabled: true });
    const click = vi.fn();
    (wrapper.get('input[type="file"]').element as HTMLInputElement).click =
      click;

    await wrapper.get(".border-dashed").trigger("click");

    expect(click).not.toHaveBeenCalled();
  });
});

describe("NieFileUploadField removal", () => {
  it("labels each remove control with the file it removes", () => {
    const wrapper = mountField({
      modelValue: [asItem(makeFile("invoice.pdf"), "a")],
    });

    expect(wrapper.get("li button").attributes("aria-label")).toBe(
      "Remove invoice.pdf",
    );
  });

  it("removes only the chosen file", async () => {
    const wrapper = mountField({
      modelValue: [
        asItem(makeFile("a.pdf", 1, 1), "a"),
        asItem(makeFile("b.pdf", 2, 2), "b"),
      ],
    });

    await wrapper.findAll("li button")[0].trigger("click");

    expect(lastEmitted(wrapper).map((file) => file.id)).toEqual(["b"]);
  });

  it("renders no list while nothing is selected", () => {
    expect(mountField().find("ul").exists()).toBe(false);
  });
});
