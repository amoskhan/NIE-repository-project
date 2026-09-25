import { ref, shallowRef } from "vue";
import type { PDFDocumentLoadingTask } from "pdfjs-dist";
import type { SyllabusPage, SyllabusSource } from "./types";

const maximumFileSize = 50 * 1024 * 1024;
const maximumPageCount = 300;

function isPdf(file: File): boolean {
  return file.type === "application/pdf" || file.name.toLowerCase().endsWith(".pdf");
}

function textFromPage(items: unknown[]): string {
  return items
    .map((item) => {
      if (typeof item !== "object" || item === null || !("str" in item)) {
        return "";
      }

      return typeof item.str === "string" ? item.str : "";
    })
    .join(" ")
    .replace(/\s+/g, " ")
    .trim();
}

export function useSyllabusSource() {
  const source = ref<SyllabusSource | null>(null);
  const error = shallowRef("");
  const isProcessing = shallowRef(false);
  const progressLabel = shallowRef("");

  async function uploadSyllabus(file: File): Promise<void> {
    error.value = "";

    if (!isPdf(file)) {
      error.value = "Choose a PDF version of the 2024 PE Syllabus.";
      return;
    }

    if (file.size > maximumFileSize) {
      error.value = "Choose a PDF smaller than 50 MB.";
      return;
    }

    isProcessing.value = true;
    progressLabel.value = "Opening syllabus…";
    let loadingTask: PDFDocumentLoadingTask | undefined;

    try {
      const [{ getDocument, GlobalWorkerOptions }, workerModule] = await Promise.all([
        import("pdfjs-dist"),
        import("pdfjs-dist/build/pdf.worker.mjs?url"),
      ]);

      GlobalWorkerOptions.workerSrc = workerModule.default;
      loadingTask = getDocument({
        data: new Uint8Array(await file.arrayBuffer()),
        stopAtErrors: true,
      });
      const document = await loadingTask.promise;

      if (document.numPages > maximumPageCount) {
        throw new Error("The PDF has more than 300 pages.");
      }

      const pages: SyllabusPage[] = [];

      for (let pageNumber = 1; pageNumber <= document.numPages; pageNumber += 1) {
        progressLabel.value = `Reading page ${pageNumber} of ${document.numPages}…`;
        const page = await document.getPage(pageNumber);
        const content = await page.getTextContent();
        const text = textFromPage(content.items);

        if (text) {
          pages.push({ pageNumber, text });
        }
      }

      if (pages.length === 0) {
        throw new Error("No selectable text was found in this PDF.");
      }

      source.value = {
        fileName: file.name,
        pageCount: document.numPages,
        pages,
      };
    } catch (reason) {
      error.value = reason instanceof Error && reason.message === "No selectable text was found in this PDF."
        ? "This PDF has no selectable text. Upload a text-based PDF of the syllabus."
        : "The PDF could not be read. Check that it is a complete, unprotected syllabus PDF.";
    } finally {
      await loadingTask?.destroy();
      isProcessing.value = false;
      progressLabel.value = "";
    }
  }

  function removeSyllabus(): void {
    source.value = null;
    error.value = "";
  }

  return { error, isProcessing, progressLabel, removeSyllabus, source, uploadSyllabus };
}
