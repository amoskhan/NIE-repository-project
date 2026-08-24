import api from "../core/api";
import type {
  WorkflowStateLog,
  WorkflowTransition,
  TransitionRequest,
} from "@/types/workflow";

const BASE = "/api/Workflow";

const workflowService = {
  async getCurrentState(
    ownerType: string,
    ownerId: string,
  ): Promise<WorkflowStateLog | null> {
    const res = await api.get(`${BASE}/${ownerType}/${ownerId}/state`);
    return res.data;
  },

  async getStateHistory(
    ownerType: string,
    ownerId: string,
  ): Promise<WorkflowStateLog[]> {
    const res = await api.get(`${BASE}/${ownerType}/${ownerId}/history`);
    return res.data;
  },

  async transitionState(
    ownerType: string,
    ownerId: string,
    request: TransitionRequest,
  ): Promise<WorkflowStateLog> {
    const res = await api.post(
      `${BASE}/${ownerType}/${ownerId}/transition`,
      request,
    );
    return res.data;
  },

  async getAvailableTransitions(
    ownerType: string,
    ownerId: string,
  ): Promise<WorkflowTransition[]> {
    const res = await api.get(
      `${BASE}/${ownerType}/${ownerId}/available-transitions`,
    );
    return res.data;
  },
};

export default workflowService;
