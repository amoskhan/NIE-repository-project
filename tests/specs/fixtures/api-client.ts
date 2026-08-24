/**
 * API Client
 * Reusable HTTP client for API testing with session token management
 */

import { APIRequestContext, request } from "@playwright/test";
import { TestConfig } from "./test-config";

export interface ApiResponse<T = any> {
  status: number;
  statusText: string;
  data: T;
  headers: { [key: string]: string };
  responseTime: number;
}

export interface LoginResponse {
  isAuthenticated: boolean;
  userId: string;
  sessionToken: string;
  userName: string;
  email: string;
  roles?: string[];
  errorMessage?: string;
}

export interface AuthCookies {
  sessionToken: string;
  userId: string;
  userName?: string;
}

/**
 * API Client class for making authenticated API requests
 */
export class ApiClient {
  private context: APIRequestContext | null = null;
  private sessionToken: string = "";
  private userId: string = "";

  constructor(
    private baseUrl: string,
    private timeout: number = TestConfig.apiTimeout,
  ) {}

  /**
   * Initialize the API request context
   */
  async init(): Promise<void> {
    this.context = await request.newContext({
      baseURL: this.baseUrl,
      timeout: this.timeout,
      ignoreHTTPSErrors: true,
      extraHTTPHeaders: {
        Accept: "application/json",
        "Content-Type": "application/json",
      },
    });
  }

  /**
   * Dispose the API request context
   */
  async dispose(): Promise<void> {
    if (this.context) {
      await this.context.dispose();
      this.context = null;
    }
  }

  /**
   * Set session token for authenticated requests
   */
  setSession(sessionToken: string, userId?: string): void {
    this.sessionToken = sessionToken;
    if (userId) {
      this.userId = userId;
    }
  }

  /**
   * Get current session token
   */
  getSessionToken(): string {
    return this.sessionToken;
  }

  /**
   * Get current user ID
   */
  getUserId(): string {
    return this.userId;
  }

  /**
   * Build headers with authentication
   */
  private getHeaders(): { [key: string]: string } {
    const headers: { [key: string]: string } = {
      Accept: "application/json",
      "Content-Type": "application/json",
    };

    if (this.sessionToken) {
      headers["X-Session-Id"] = this.sessionToken;
      // Adjust cookie names based on your session implementation
      headers["Cookie"] =
        `SessionToken=${this.sessionToken}; UserId=${this.userId}`;
    }

    return headers;
  }

  /**
   * Make a GET request
   */
  async get<T = any>(
    endpoint: string,
    params?: Record<string, string | number>,
  ): Promise<ApiResponse<T>> {
    if (!this.context) {
      throw new Error("ApiClient not initialized. Call init() first.");
    }

    const startTime = Date.now();

    let url = endpoint;
    if (params) {
      const searchParams = new URLSearchParams();
      for (const [key, value] of Object.entries(params)) {
        searchParams.append(key, String(value));
      }
      url = `${endpoint}?${searchParams.toString()}`;
    }

    const response = await this.context.get(url, {
      headers: this.getHeaders(),
    });

    const responseTime = Date.now() - startTime;

    let data: T;
    try {
      data = await response.json();
    } catch {
      data = (await response.text()) as unknown as T;
    }

    return {
      status: response.status(),
      statusText: response.statusText(),
      data,
      headers: response.headers(),
      responseTime,
    };
  }

  /**
   * Make a POST request
   */
  async post<T = any>(
    endpoint: string,
    body?: Record<string, any>,
  ): Promise<ApiResponse<T>> {
    if (!this.context) {
      throw new Error("ApiClient not initialized. Call init() first.");
    }

    const startTime = Date.now();

    const response = await this.context.post(endpoint, {
      headers: this.getHeaders(),
      data: body,
    });

    const responseTime = Date.now() - startTime;

    let data: T;
    try {
      data = await response.json();
    } catch {
      data = (await response.text()) as unknown as T;
    }

    return {
      status: response.status(),
      statusText: response.statusText(),
      data,
      headers: response.headers(),
      responseTime,
    };
  }

  /**
   * Make a PUT request
   */
  async put<T = any>(
    endpoint: string,
    body?: Record<string, any>,
  ): Promise<ApiResponse<T>> {
    if (!this.context) {
      throw new Error("ApiClient not initialized. Call init() first.");
    }

    const startTime = Date.now();

    const response = await this.context.put(endpoint, {
      headers: this.getHeaders(),
      data: body,
    });

    const responseTime = Date.now() - startTime;

    let data: T;
    try {
      data = await response.json();
    } catch {
      data = (await response.text()) as unknown as T;
    }

    return {
      status: response.status(),
      statusText: response.statusText(),
      data,
      headers: response.headers(),
      responseTime,
    };
  }

  /**
   * Make a DELETE request
   */
  async delete<T = any>(endpoint: string): Promise<ApiResponse<T>> {
    if (!this.context) {
      throw new Error("ApiClient not initialized. Call init() first.");
    }

    const startTime = Date.now();

    const response = await this.context.delete(endpoint, {
      headers: this.getHeaders(),
    });

    const responseTime = Date.now() - startTime;

    let data: T;
    try {
      data = await response.json();
    } catch {
      data = (await response.text()) as unknown as T;
    }

    return {
      status: response.status(),
      statusText: response.statusText(),
      data,
      headers: response.headers(),
      responseTime,
    };
  }
}

/**
 * Create an API client for the Auth API
 */
export function createAuthApiClient(): ApiClient {
  return new ApiClient(TestConfig.authApiUrl);
}

/**
 * Create an API client for the Main API
 */
export function createApiClient(): ApiClient {
  return new ApiClient(TestConfig.mainApiUrl);
}

export default { ApiClient, createApiClient, createAuthApiClient };
