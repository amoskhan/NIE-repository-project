import axios from "axios";
import api from "../core/api";

export interface ReportTypeDefinition {
  id: string;
  name: string;
  description: string;
  category: string;
  icon: string;
  filters: ReportFilter[];
  pageSetup?: ReportPageSetupDefinition;
}

export interface ReportFilter {
  name: string;
  label: string;
  type: "dropdown" | "daterange" | "number" | "text";
  options?: string[];
}

export type ReportPageFormat = "A4" | "A3" | "A5" | "Letter" | "Legal";
export type ReportPageOrientation = "Portrait" | "Landscape";

export interface ReportPageSetupDefinition {
  defaultFormat: ReportPageFormat;
  defaultOrientation: ReportPageOrientation;
  allowFormatChange: boolean;
  allowOrientationChange: boolean;
  formats: ReportPageFormat[];
  orientations: ReportPageOrientation[];
}

export interface ReportRequest {
  reportType: string;
  status?: string;
  dateFrom?: string | null;
  dateTo?: string | null;
  vendorId?: string | null;
  category?: string;
  userId?: string;
  /** Paper format. Defaults to A4 if omitted. */
  format?: ReportPageFormat;
  /** Page orientation. Defaults to Portrait if omitted. */
  orientation?: ReportPageOrientation;
}

interface RequestOptions {
  signal?: AbortSignal;
}

export const REPORT_PAGE_FORMATS: ReportPageFormat[] = [
  "A4",
  "A3",
  "A5",
  "Letter",
  "Legal",
];

export const REPORT_PAGE_ORIENTATIONS: ReportPageOrientation[] = [
  "Portrait",
  "Landscape",
];

export const DEFAULT_REPORT_PAGE_SETUP: ReportPageSetupDefinition = {
  defaultFormat: "A4",
  defaultOrientation: "Portrait",
  allowFormatChange: true,
  allowOrientationChange: true,
  formats: REPORT_PAGE_FORMATS,
  orientations: REPORT_PAGE_ORIENTATIONS,
};

export const DEFAULT_REPORT_TYPES: ReportTypeDefinition[] = [
  {
    id: "po-summary",
    name: "Purchase Order Summary",
    description: "Purchase order status, vendor, and amount summary.",
    category: "Procurement",
    icon: "receipt_long",
    filters: [
      {
        name: "status",
        label: "Status",
        type: "dropdown",
        options: [
          "All",
          "Draft",
          "PendingApproval",
          "Approved",
          "Rejected",
          "Ordered",
          "Completed",
        ],
      },
      { name: "dateRange", label: "Date Range", type: "daterange" },
    ],
  },
  {
    id: "vendor-analysis",
    name: "Vendor Analysis",
    description: "Vendor order volume and approved spend.",
    category: "Procurement",
    icon: "storefront",
    filters: [
      { name: "vendorId", label: "Vendor ID", type: "number" },
      { name: "dateRange", label: "Date Range", type: "daterange" },
    ],
  },
  {
    id: "spending-by-dept",
    name: "Spending by Requester",
    description: "Approved spend grouped by requester.",
    category: "Procurement",
    icon: "payments",
    filters: [{ name: "dateRange", label: "Date Range", type: "daterange" }],
  },
  {
    id: "approval-timeline",
    name: "Approval Timeline",
    description: "Approval actions and most recent processing date.",
    category: "Procurement",
    icon: "approval",
    filters: [{ name: "dateRange", label: "Date Range", type: "daterange" }],
  },
  {
    id: "audit-trail",
    name: "Audit Trail",
    description: "Audit events by period, category, and user.",
    category: "Audit",
    icon: "history",
    pageSetup: {
      ...DEFAULT_REPORT_PAGE_SETUP,
      defaultOrientation: "Landscape",
    },
    filters: [
      { name: "dateRange", label: "Date Range", type: "daterange" },
      {
        name: "category",
        label: "Category",
        type: "dropdown",
        options: [
          "All",
          "Data",
          "Authentication",
          "AccessControl",
          "FileOperation",
          "DataTransfer",
          "System",
        ],
      },
    ],
  },
  {
    id: "user-activity",
    name: "User Activity",
    description: "User activity counts and most recent action.",
    category: "Audit",
    icon: "person_search",
    filters: [
      { name: "dateRange", label: "Date Range", type: "daterange" },
      { name: "userId", label: "User ID", type: "text" },
    ],
  },
];

export function normalizeReportPageSetup(
  pageSetup?: Partial<ReportPageSetupDefinition>,
): ReportPageSetupDefinition {
  const defaultFormat = normalizeReportPageFormat(pageSetup?.defaultFormat)
    ?? DEFAULT_REPORT_PAGE_SETUP.defaultFormat;
  const defaultOrientation = normalizeReportPageOrientation(pageSetup?.defaultOrientation)
    ?? DEFAULT_REPORT_PAGE_SETUP.defaultOrientation;
  const formats = normalizeReportPageFormats(pageSetup?.formats);
  const orientations = normalizeReportPageOrientations(pageSetup?.orientations);

  return {
    defaultFormat,
    defaultOrientation,
    allowFormatChange:
      pageSetup?.allowFormatChange ?? DEFAULT_REPORT_PAGE_SETUP.allowFormatChange,
    allowOrientationChange:
      pageSetup?.allowOrientationChange
      ?? DEFAULT_REPORT_PAGE_SETUP.allowOrientationChange,
    formats: formats.includes(defaultFormat) ? formats : [...formats, defaultFormat],
    orientations: orientations.includes(defaultOrientation)
      ? orientations
      : [...orientations, defaultOrientation],
  };
}

export function normalizeReportPageFormat(
  format?: string | null,
): ReportPageFormat | null {
  switch (format?.trim().toUpperCase()) {
    case "A3":
      return "A3";
    case "A4":
      return "A4";
    case "A5":
      return "A5";
    case "LETTER":
      return "Letter";
    case "LEGAL":
      return "Legal";
    default:
      return null;
  }
}

export function normalizeReportPageOrientation(
  orientation?: string | null,
): ReportPageOrientation | null {
  switch (orientation?.trim().toUpperCase()) {
    case "PORTRAIT":
      return "Portrait";
    case "LANDSCAPE":
      return "Landscape";
    default:
      return null;
  }
}

function normalizeReportPageFormats(
  formats?: readonly string[],
): ReportPageFormat[] {
  const normalized = (formats ?? REPORT_PAGE_FORMATS)
    .map((format) => normalizeReportPageFormat(format))
    .filter((format): format is ReportPageFormat => format !== null);
  return normalized.length > 0 ? [...new Set(normalized)] : REPORT_PAGE_FORMATS;
}

function normalizeReportPageOrientations(
  orientations?: readonly string[],
): ReportPageOrientation[] {
  const normalized = (orientations ?? REPORT_PAGE_ORIENTATIONS)
    .map((orientation) => normalizeReportPageOrientation(orientation))
    .filter((orientation): orientation is ReportPageOrientation => orientation !== null);
  return normalized.length > 0
    ? [...new Set(normalized)]
    : REPORT_PAGE_ORIENTATIONS;
}

const reportService = {
  async getReportTypes(
    options: RequestOptions = {},
  ): Promise<ReportTypeDefinition[]> {
    const res = await api.get<ReportTypeDefinition[]>("/api/Report/types", {
      signal: options.signal,
    });
    return res.data;
  },

  async generatePreview(
    reportType: string,
    request: ReportRequest,
    options: RequestOptions = {},
  ): Promise<string> {
    const res = await api.post<string>(
      "/api/Report/preview",
      { ...request, reportType },
      { responseType: "text", signal: options.signal },
    );
    return res.data;
  },

  async generatePdfBlob(
    reportType: string,
    request: ReportRequest,
    options: RequestOptions = {},
  ): Promise<Blob> {
    const res = await api.post<Blob>(
      "/api/Report/pdf",
      { ...request, reportType },
      { responseType: "blob", signal: options.signal },
    );
    return res.data;
  },

  async downloadPdf(
    reportType: string,
    request: ReportRequest,
    options: RequestOptions = {},
  ): Promise<void> {
    const res = await api.post<Blob>(
      "/api/Report/download",
      { ...request, reportType },
      { responseType: "blob", signal: options.signal },
    );
    const url = URL.createObjectURL(res.data);
    const a = document.createElement("a");
    a.href = url;
    a.download = `${reportType}_${new Date().toISOString().slice(0, 10)}.pdf`;
    a.click();
    URL.revokeObjectURL(url);
  },
};

export function isReportRequestCanceled(error: unknown): boolean {
  return axios.isCancel(error)
    || (error instanceof DOMException && error.name === "AbortError");
}

export default reportService;
