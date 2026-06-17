import axios from "axios";
import { getSession, signOut } from "next-auth/react";

const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL,
});

api.interceptors.request.use(async (config) => {
  const session = await getSession();
  if (session?.accessToken) {
    config.headers.Authorization = `Bearer ${session.accessToken}`;
  }
  return config;
});

let signingOut = false;

api.interceptors.response.use(
  (response) => response,
  (error) => {
    const isAlreadyOnLogin =
      typeof window !== "undefined" &&
      window.location.pathname.startsWith("/login");

    if (error.response?.status === 401 && !signingOut && !isAlreadyOnLogin) {
      signingOut = true;
      signOut({ redirect: false }).then(() => {
        signingOut = false;
        window.location.href = "/login";
      });
    }
    return Promise.reject(error);
  }
);

export default api;

// Pulls a human-readable message out of an API error. Prefers the first
// FluentValidation message (ASP.NET ValidationProblemDetails.errors), then
// ProblemDetails.detail/title, falling back to the supplied default.
export function apiErrorMessage(err: unknown, fallback: string): string {
  if (axios.isAxiosError(err) && err.response?.data) {
    const data = err.response.data as {
      title?: string;
      detail?: string;
      errors?: Record<string, string[]>;
    };
    const firstValidation = data.errors
      ? Object.values(data.errors).flat()[0]
      : undefined;
    return firstValidation ?? data.detail ?? data.title ?? fallback;
  }
  return fallback;
}
