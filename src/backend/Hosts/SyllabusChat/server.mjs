import http from "node:http";
import { pathToFileURL } from "node:url";
import { AnswerError, createAnswerer } from "./answer.mjs";
import { createRetriever, fileName, readSyllabus } from "./syllabus.mjs";

function json(res, status, value) {
  res.writeHead(status, { "Content-Type": "application/json", "Cache-Control": "no-store", "X-Content-Type-Options": "nosniff" });
  res.end(JSON.stringify(value));
}

export function createServer(answerer, pageCount) {
  let activeRequests = 0;
  return http.createServer(async (req, res) => {
    const pathname = new URL(req.url, "http://localhost").pathname;
    if (req.method === "GET" && pathname === "/health") {
      return json(res, 200, { status: "ok", syllabusPages: pageCount, llmConfigured: answerer.configured });
    }
    if (pathname !== "/chat") return json(res, 404, { message: "Not found." });
    if (req.method !== "POST") { res.setHeader("Allow", "POST"); return json(res, 405, { message: "Use POST." }); }
    if (req.headers["sec-fetch-site"] === "cross-site") return json(res, 403, { message: "Cross-site requests are not allowed." });
    if (!req.headers["content-type"]?.startsWith("application/json")) return json(res, 415, { message: "Send JSON." });
    if (Number(req.headers["content-length"]) > 16000) return json(res, 413, { message: "The request is too large." });
    if (activeRequests >= 4) return json(res, 429, { message: "The assistant is busy. Please try again shortly." });
    activeRequests++;
    try {
      const chunks = [];
      let size = 0;
      for await (const chunk of req) {
        size += chunk.length;
        if (size > 16000) throw new AnswerError("invalid_request", 413, "The request is too large.");
        chunks.push(chunk);
      }
      let input;
      try { input = JSON.parse(Buffer.concat(chunks).toString("utf8")); } catch { throw new AnswerError("invalid_request", 400, "Send a valid JSON question."); }
      if (!input || typeof input.question !== "string" || !input.question.trim() || input.question.length > 1000) {
        throw new AnswerError("invalid_request", 400, "Enter a question of up to 1,000 characters.");
      }
      const history = input.history ?? [];
      if (!Array.isArray(history) || history.length > 6 || history.some(message => !message
        || !["user", "assistant"].includes(message.role) || typeof message.content !== "string" || message.content.length > 2500)) {
        throw new AnswerError("invalid_request", 400, "Conversation context is invalid.");
      }
      const result = await answerer.answer(input.question.trim(), history);
      json(res, 200, { ...result, fileName });
    } catch (error) {
      json(res, error instanceof AnswerError ? error.status : 500, {
        code: error instanceof AnswerError ? error.code : "internal_error",
        message: error instanceof AnswerError ? error.message : "The answer service encountered a problem. Please retry.",
      });
    } finally {
      activeRequests--;
    }
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  try {
    const pages = await readSyllabus();
    // Secrets are consumed only by this server and never returned, logged, or bundled for the browser.
    const answerer = createAnswerer({
      baseUrl: process.env.SYLLABUS_LLM_BASE_URL,
      model: process.env.SYLLABUS_LLM_MODEL,
      apiKey: process.env.SYLLABUS_LLM_API_KEY,
      retrieve: createRetriever(pages),
    });
    const server = createServer(answerer, pages.length);
    server.requestTimeout = 15000;
    server.headersTimeout = 10000;
    server.listen(Number(process.env.PORT || 15100), "0.0.0.0", () => console.log("Syllabus answer service is listening."));
  } catch {
    console.error("Unable to start the answer service. Check the syllabus file and installed dependencies.");
    process.exitCode = 1;
  }
}
