import axios, {
  type AxiosInstance,
  type AxiosRequestConfig,
  type AxiosError,
} from "axios";
import { ref, readonly } from "vue";
import { useAuth } from "./useAuth";
import { FRONTEND_CONSTANTS } from "../config";

interface ApiResponse<T = unknown> {
  success: boolean;
  data: T;
  message?: string;
}

interface ApiError {
  statusCode: number;
  message: string;
  details?: string;
}

const isLoading = ref(false);
const error = ref<string | null>(null);

export function useApi(baseURL?: string) {
  const { getSessionToken, logout } = useAuth();

  const client: AxiosInstance = axios.create({
    baseURL: baseURL || FRONTEND_CONSTANTS.backend.main,
    timeout: 30000,
    headers: {
      "Content-Type": "application/json",
    },
  });

  // Request interceptor
  client.interceptors.request.use(
    (config) => {
      const token = getSessionToken();
      if (token) {
        config.headers["X-Session-Id"] = token;
      }
      return config;
    },
    (err) => Promise.reject(err),
  );

  // Response interceptor
  client.interceptors.response.use(
    (response) => response,
    (err: AxiosError<ApiError>) => {
      if (err.response?.status === 401) {
        logout();
      }
      return Promise.reject(err);
    },
  );

  async function get<T>(
    url: string,
    config?: AxiosRequestConfig,
  ): Promise<ApiResponse<T>> {
    isLoading.value = true;
    error.value = null;
    try {
      const response = await client.get<ApiResponse<T>>(url, config);
      return response.data;
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      error.value = axiosError.response?.data?.message || axiosError.message;
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  async function post<T>(
    url: string,
    data?: unknown,
    config?: AxiosRequestConfig,
  ): Promise<ApiResponse<T>> {
    isLoading.value = true;
    error.value = null;
    try {
      const response = await client.post<ApiResponse<T>>(url, data, config);
      return response.data;
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      error.value = axiosError.response?.data?.message || axiosError.message;
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  async function put<T>(
    url: string,
    data?: unknown,
    config?: AxiosRequestConfig,
  ): Promise<ApiResponse<T>> {
    isLoading.value = true;
    error.value = null;
    try {
      const response = await client.put<ApiResponse<T>>(url, data, config);
      return response.data;
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      error.value = axiosError.response?.data?.message || axiosError.message;
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  async function del<T>(
    url: string,
    config?: AxiosRequestConfig,
  ): Promise<ApiResponse<T>> {
    isLoading.value = true;
    error.value = null;
    try {
      const response = await client.delete<ApiResponse<T>>(url, config);
      return response.data;
    } catch (err) {
      const axiosError = err as AxiosError<ApiError>;
      error.value = axiosError.response?.data?.message || axiosError.message;
      throw err;
    } finally {
      isLoading.value = false;
    }
  }

  return {
    client,
    isLoading: readonly(isLoading),
    error: readonly(error),
    get,
    post,
    put,
    delete: del,
  };
}
