import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import { getDocument } from "pdfjs-dist/legacy/build/pdf.mjs";

const moduleId = "virtual:pe-syllabus";
const resolvedId = `\0${moduleId}`;
const fileName = "2024 Physical Education Primary Secondary and PreUniversity Syllabus (1).pdf";
const pdfPath = fileURLToPath(new URL(`../../../../../upload/${fileName}`, import.meta.url));

// Read the supplied syllabus during development/build, never in the visitor's browser.
async function extractSyllabus() {
  const task = getDocument({
    data: new Uint8Array(await readFile(pdfPath)),
    useSystemFonts: true,
    stopAtErrors: true,
    verbosity: 0,
  });

  try {
    const pdf = await task.promise;
    const pages = [];
    for (let pageNumber = 1; pageNumber <= pdf.numPages; pageNumber += 1) {
      const page = await pdf.getPage(pageNumber);
      const content = await page.getTextContent();
      const text = content.items
        .filter((item) => "str" in item)
        .map((item) => item.str)
        .join(" ")
        .replace(/\s+/g, " ")
        .trim();
      pages.push({ pageNumber, text });
      page.cleanup();
    }
    if (!pages.some((page) => page.text)) {
      throw new Error("The project syllabus has no readable text.");
    }
    return { origin: "project", fileName, pageCount: pdf.numPages, pages };
  } finally {
    await task.destroy();
  }
}

export function syllabusPlugin() {
  let extraction;
  return {
    name: "project-pe-syllabus",
    resolveId(id) {
      if (id === moduleId) return resolvedId;
    },
    async load(id) {
      if (id !== resolvedId) return;
      this.addWatchFile(pdfPath);
      extraction ??= extractSyllabus().catch((error) => {
        extraction = undefined;
        throw error;
      });
      return `export default ${JSON.stringify(await extraction)};`;
    },
    handleHotUpdate({ file, server }) {
      if (file !== pdfPath) return;
      extraction = undefined;
      const module = server.moduleGraph.getModuleById(resolvedId);
      if (module) server.moduleGraph.invalidateModule(module);
      server.ws.send({ type: "full-reload" });
      return [];
    },
  };
}
