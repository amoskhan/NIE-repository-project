import api from "../core/api";

export type FeedbackRating = "1" | "5";

export interface FeedbackSubmitRequest {
  function_id: string;
  rating: FeedbackRating;
  feedback: string;
  page: string;
}

export interface FeedbackSubmitResponse {
  acknowledged: boolean;
}

const feedbackService = {
  async submit(request: FeedbackSubmitRequest): Promise<FeedbackSubmitResponse> {
    const response = await api.post<FeedbackSubmitResponse>(
      "/api/Feedback/Submit",
      request,
    );
    return response.data;
  },
};

export default feedbackService;
