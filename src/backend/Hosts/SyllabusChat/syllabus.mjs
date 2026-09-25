import { readFile } from "node:fs/promises";
import { getDocument } from "pdfjs-dist/legacy/build/pdf.mjs";

export const fileName = "2024 Physical Education Primary Secondary and PreUniversity Syllabus (1).pdf";

export async function readSyllabus() {
  const task = getDocument({
    data: new Uint8Array(await readFile(new URL(`../../../../upload/${fileName}`, import.meta.url))),
    useSystemFonts: true, stopAtErrors: true, verbosity: 0,
  });
  try {
    const pdf = await task.promise;
    const pages = [];
    for (let pageNumber = 1; pageNumber <= pdf.numPages; pageNumber++) {
      const page = await pdf.getPage(pageNumber);
      const content = await page.getTextContent();
      const text = content.items.filter(item => "str" in item)
        .map(item => item.str + (item.hasEOL ? "\n" : " "))
        .join("").replace(/[ \t]+/g, " ").replace(/\n\s*\n/g, "\n").trim();
      pages.push({ pageNumber, printedPage: text.match(/^(\d+)\s/)?.[1] ?? null, text });
      page.cleanup();
    }
    if (!pages.some(page => page.text)) throw new Error("Syllabus has no readable text.");
    return pages;
  } finally {
    await task.destroy();
  }
}

const stopWords = new Set("a an and are at about based be by can could do does for from give how i in introduce introduced introduction is it learn learnt learned learning me of on or please provide school should student students syllabus teach taught teaching tell that the their them they this to what when where which who will with would year years level levels".split(" "));
function words(text) {
  return (text.toLowerCase().replace(/\bfms\b/g, "fundamental movement motor skills").match(/[a-z0-9]+/g) ?? [])
    .filter(word => word.length > 2 && !stopWords.has(word))
    .map(word => word.length > 5 && word.endsWith("ing") ? word.slice(0, -3) : word.length > 4 && word.endsWith("s") ? word.slice(0, -1) : word);
}

export function createRetriever(pages) {
  const indexed = pages.map(page => ({ ...page, terms: words(page.text) }));
  return (question, history = []) => {
    let terms = [...new Set(words(question))];
    if (terms.length < 2 && /\b(it|that|those|they|them|more|instead|what about)\b/i.test(question)) {
      const previousQuestion = [...history].reverse().find(message => message.role === "user");
      terms = [...new Set([...terms, ...words(previousQuestion?.content ?? "")])];
    }
    const weights = new Map(terms.map(term => [term, Math.log(1 + pages.length / (1 + indexed.filter(page => page.terms.includes(term)).length))]));
    const ranked = indexed.map(page => {
      let score = terms.reduce((total, term) => total + (weights.get(term) ?? 0) * Math.min(3, page.terms.filter(word => word === term).length), 0);
      if (score > 0 && /\b(when|which (year|level)|start|introduc)\b/i.test(question)
        && /PRIMARY\s+[1-6]\s*[–-]/.test(page.text)) score *= 1.6;
      return { page, score };
    }).filter(item => item.score > 0).sort((a, b) => b.score - a.score);
    // Whole pages retain school-level headings, table labels, and footnotes.
    const selected = ranked.slice(0, 8).map(({page}) => page);
    let budget = 45000;
    return selected.map(({ terms: _terms, ...page }) => {
      const text = page.text.slice(0, Math.min(9000, budget));
      budget -= text.length;
      return { ...page, text };
    }).filter(page => page.text);
  };
}
