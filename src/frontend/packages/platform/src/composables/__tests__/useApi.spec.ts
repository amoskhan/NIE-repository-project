import {
  AxiosError,
  type AxiosAdapter,
  type InternalAxiosRequestConfig,
} from "axios";
import { afterEach, describe, expect, it } from "vitest";
import { FRONTEND_CONSTANTS } from "../../config";
import { useApi } from "../useApi";
import { useAuth } from "../useAuth";

const originalLocation = Object.getOwnPropertyDescriptor(window, "location");

function stubLocation(): { href: string } {
  const location = { href: "http://localhost:8002/", hostname: "localhost" };
  Object.defineProperty(window, "location", {
    configurable: true,
    writable: true,
    value: location,
  });
  return location;
}

interface RecordingAdapter {
  adapter: AxiosAdapter;
  requests: InternalAxiosRequestConfig[];
}

function recordingAdapter(payload: unknown = { id: 1 }): RecordingAdapter {
  const requests: InternalAxiosRequestConfig[] = [];
  return {
    requests,
    adapter: (config) => {
      requests.push(config);
      return Promise.resolve({
        data: { success: true, data: payload },
        status: 200,
        statusText: "OK",
        headers: {},
        config,
      });
    },
  };
}

function failingAdapter(status: number, message: string): AxiosAdapter {
  return (config) =>
    Promise.reject(
      new AxiosError(`Request failed with status code ${status}`, "ERR", config, undefined, {
        data: { statusCode: status, message },
        status,
        statusText: "Error",
        headers: {},
        config,
      }),
    );
}

afterEach(() => {
  useAuth().clearSession();
  if (originalLocation) {
    Object.defineProperty(window, "location", originalLocation);
  }
});

describe("useApi client configuration", () => {
  it("defaults to the main backend gateway", () => {
    expect(useApi().client.defaults.baseURL).toBe(FRONTEND_CONSTANTS.backend.main);
  });

  it("honours an explicit base URL", () => {
    expect(useApi("/api-auth").client.defaults.baseURL).toBe("/api-auth");
  });
});

describe("useApi request interceptor", () => {
  it("attaches the session token as X-Session-Id", async () => {
    useAuth().setSessionToken("session-abc");
    const api = useApi("/api-main");
    const { adapter, requests } = recordingAdapter();
    api.client.defaults.adapter = adapter;

    await api.get("/users");

    expect(requests).toHaveLength(1);
    expect(requests[0].headers["X-Session-Id"]).toBe("session-abc");
  });

  it("omits the header when there is no session", async () => {
    const api = useApi("/api-main");
    const { adapter, requests } = recordingAdapter();
    api.client.defaults.adapter = adapter;

    await api.get("/users");

    expect(requests[0].headers["X-Session-Id"]).toBeUndefined();
  });
});

describe("useApi verbs", () => {
  it("maps each helper onto the matching HTTP method and unwraps the envelope", async () => {
    const api = useApi("/api-main");
    const { adapter, requests } = recordingAdapter({ id: 7 });
    api.client.defaults.adapter = adapter;

    const results = [
      await api.get("/users"),
      await api.post("/users", { name: "Ada" }),
      await api.put("/users/7", { name: "Ada" }),
      await api.delete("/users/7"),
    ];

    expect(requests.map((request) => request.method)).toEqual([
      "get",
      "post",
      "put",
      "delete",
    ]);
    expect(requests[1].data).toBe(JSON.stringify({ name: "Ada" }));
    for (const result of results) {
      expect(result).toEqual({ success: true, data: { id: 7 } });
    }
  });

  it("forwards per-call request configuration", async () => {
    const api = useApi("/api-main");
    const { adapter, requests } = recordingAdapter();
    api.client.defaults.adapter = adapter;

    await api.get("/users", { params: { page: 2 } });

    expect(requests[0].params).toEqual({ page: 2 });
  });
});

describe("useApi loading and error state", () => {
  it("raises isLoading for the duration of a request", async () => {
    const api = useApi("/api-main");
    let release: () => void = () => {};
    const gate = new Promise<void>((resolve) => {
      release = () => {
        resolve();
      };
    });
    api.client.defaults.adapter = async (config) => {
      await gate;
      return {
        data: { success: true, data: null },
        status: 200,
        statusText: "OK",
        headers: {},
        config,
      };
    };

    const pending = api.get("/slow");
    expect(api.isLoading.value).toBe(true);

    release();
    await pending;

    expect(api.isLoading.value).toBe(false);
  });

  it("surfaces the server message and still rejects", async () => {
    const api = useApi("/api-main");
    api.client.defaults.adapter = failingAdapter(422, "Name is required.");

    await expect(api.post("/users", {})).rejects.toBeInstanceOf(AxiosError);

    expect(api.error.value).toBe("Name is required.");
    expect(api.isLoading.value).toBe(false);
  });

  it("falls back to the transport message when the body carries none", async () => {
    const api = useApi("/api-main");
    api.client.defaults.adapter = (config) =>
      Promise.reject(new AxiosError("Network Error", "ERR_NETWORK", config));

    await expect(api.put("/users/1", {})).rejects.toBeInstanceOf(AxiosError);

    expect(api.error.value).toBe("Network Error");
  });

  it("clears a previous error when the next request succeeds", async () => {
    const api = useApi("/api-main");
    api.client.defaults.adapter = failingAdapter(500, "Server exploded.");
    await expect(api.delete("/users/1")).rejects.toBeInstanceOf(AxiosError);
    expect(api.error.value).toBe("Server exploded.");

    api.client.defaults.adapter = recordingAdapter().adapter;
    await api.get("/users");

    expect(api.error.value).toBeNull();
  });
});

describe("useApi response interceptor", () => {
  it("signs the user out on 401 and redirects to the auth app", async () => {
    const location = stubLocation();
    const auth = useAuth();
    auth.setSessionToken("session-abc");
    auth.setUser({
      id: "1",
      email: "staff@nie.edu.sg",
      name: "Staff",
      roles: ["Staff"],
    });
    const api = useApi("/api-main");
    api.client.defaults.adapter = failingAdapter(401, "Session expired.");

    await expect(api.get("/users")).rejects.toBeInstanceOf(AxiosError);

    expect(auth.getSessionToken()).toBeUndefined();
    expect(auth.user.value).toBeNull();
    expect(location.href).toBe(FRONTEND_CONSTANTS.apps.auth);
  });

  it("leaves the session alone for other failures", async () => {
    const location = stubLocation();
    const auth = useAuth();
    auth.setSessionToken("session-abc");
    const api = useApi("/api-main");
    api.client.defaults.adapter = failingAdapter(403, "Forbidden.");

    await expect(api.get("/users")).rejects.toBeInstanceOf(AxiosError);

    expect(auth.getSessionToken()).toBe("session-abc");
    expect(location.href).toBe("http://localhost:8002/");
  });
});
