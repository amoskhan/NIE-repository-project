import api from "../core/api";
import type { PurchaseOrderStatusName } from "@/types/procurementStatus";
import type {
  NieDataTableFilterOptionPage,
  NieDataTableFilterOptionsRequest,
  NieDataTableQuery,
} from "@nie/ui";
import type { ServerDataTablePage } from "@/composables/data-tables/useServerDataTable";
import {
  toApiDataTableRequest,
  toApiFilterOptionsRequest,
} from "../core/dataTableApi";

export interface PurchaseOrderLineDto {
  id?: string;
  lineNumber: number;
  itemName: string;
  description?: string | null;
  unitOfMeasure?: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  catalogItemId?: string | null;
}

export interface PurchaseOrderApprovalDto {
  id?: string;
  approvalStage: string;
  stageOrder: number;
  approverId?: string | null;
  approverName?: string | null;
  action?: number | null;
  actionDate?: string | null;
  comments?: string | null;
}

export interface PurchaseOrderDocumentDto {
  id: string;
  filePath: string;
  fileSize: number;
  userFileName: string;
  documentType?: string | null;
}

export interface PurchaseOrderDto {
  id?: string;
  poNumber?: string;
  requestedBy?: string;
  requestedByName?: string | null;
  requestDate?: string;
  deliveryAddress?: string | null;
  expectedDeliveryDate?: string | null;
  status?: number;
  statusName?: PurchaseOrderStatusName;
  notes?: string | null;
  totalAmount?: number;
  rejectionReason?: string | null;
  vendorId: string;
  vendorName?: string | null;
  lines: PurchaseOrderLineDto[];
  approvals?: PurchaseOrderApprovalDto[];
  documents?: PurchaseOrderDocumentDto[];
  createdOn?: string | null;
  updatedOn?: string | null;
  createdBy?: string | null;
  updatedBy?: string | null;
}

export interface ApprovalActionDto {
  purchaseOrderId: string;
  action: number;
  comments?: string | null;
}

export interface PurchaseOrderSearchDto {
  search?: string | null;
  status?: number | null;
  vendorId?: string | null;
  fromDate?: string | null;
  toDate?: string | null;
  page: number;
  pageSize: number;
  sortBy?: string | null;
  sortDescending: boolean;
}

export interface SpendOverviewDto {
  pendingApprovals: number;
  monthlySpend: number;
  recentOrders: number;
  totalVendors: number;
  totalOrders: number;
  totalSpend: number;
  monthlySpendTrend: { month: string; amount: number }[];
  statusBreakdown: { status: string; count: number }[];
  topVendors: { vendorName: string; totalSpend: number; orderCount: number }[];
  recentOrdersList: {
    id: string;
    poNumber: string;
    vendorName: string;
    totalAmount: number;
    status: PurchaseOrderStatusName;
    requestDate: string;
  }[];
}

export interface SearchResult {
  items: PurchaseOrderDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}

const purchaseOrderService = {
  async searchTable(
    query: NieDataTableQuery,
  ): Promise<ServerDataTablePage<PurchaseOrderDto>> {
    return (
      await api.post<ServerDataTablePage<PurchaseOrderDto>>(
        "/api/PurchaseOrder/SearchTable",
        toApiDataTableRequest(query),
      )
    ).data;
  },

  async getFilterOptions(
    request: NieDataTableFilterOptionsRequest,
  ): Promise<NieDataTableFilterOptionPage> {
    return (
      await api.post<NieDataTableFilterOptionPage>(
        "/api/PurchaseOrder/GetFilterOptions",
        toApiFilterOptionsRequest(request),
      )
    ).data;
  },

  async getAll(): Promise<PurchaseOrderDto[]> {
    const response = await api.get<PurchaseOrderDto[]>(
      "/api/PurchaseOrder/GetAll",
    );
    return response.data;
  },

  async getById(id: string): Promise<PurchaseOrderDto> {
    const response = await api.get<PurchaseOrderDto>(
      `/api/PurchaseOrder/Get/${id}`,
    );
    return response.data;
  },

  async save(dto: PurchaseOrderDto): Promise<PurchaseOrderDto> {
    const endpoint = dto.id
      ? "/api/PurchaseOrder/Edit"
      : "/api/PurchaseOrder/Save";
    const response = await api.post<PurchaseOrderDto>(endpoint, dto);
    return response.data;
  },

  async submit(id: string): Promise<PurchaseOrderDto> {
    const response = await api.post<PurchaseOrderDto>(
      `/api/PurchaseOrder/Submit/${id}`,
    );
    return response.data;
  },

  async processApproval(dto: ApprovalActionDto): Promise<PurchaseOrderDto> {
    const response = await api.post<PurchaseOrderDto>(
      "/api/PurchaseOrder/ProcessApproval",
      dto,
    );
    return response.data;
  },

  async delete(id: string): Promise<void> {
    await api.post(`/api/PurchaseOrder/Delete/${id}`);
  },

  async search(filter: PurchaseOrderSearchDto): Promise<SearchResult> {
    const response = await api.post<SearchResult>(
      "/api/PurchaseOrder/Search",
      filter,
    );
    return response.data;
  },

  async getSpendOverview(): Promise<SpendOverviewDto> {
    const response = await api.get<SpendOverviewDto>(
      "/api/PurchaseOrder/GetSpendOverview",
    );
    return response.data;
  },

  async getPendingApprovals(): Promise<PurchaseOrderDto[]> {
    const response = await api.get<PurchaseOrderDto[]>(
      "/api/PurchaseOrder/GetPendingApprovals",
    );
    return response.data;
  },

  async uploadDocument(
    purchaseOrderId: string,
    file: File,
    documentType?: string,
  ): Promise<PurchaseOrderDocumentDto> {
    const formData = new FormData();
    formData.append("file", file);
    const params = new URLSearchParams();
    params.append("purchaseOrderId", purchaseOrderId.toString());
    if (documentType) params.append("documentType", documentType);
    const response = await api.post<PurchaseOrderDocumentDto>(
      `/api/PurchaseOrder/UploadDocument?${params.toString()}`,
      formData,
      { headers: { "Content-Type": "multipart/form-data" } },
    );
    return response.data;
  },

  async deleteDocument(documentId: string): Promise<void> {
    await api.post(`/api/PurchaseOrder/DeleteDocument/${documentId}`);
  },
};

export default purchaseOrderService;
