import { renderHook } from "@testing-library/react";
import { useSession } from "next-auth/react";
import { useIsAdmin } from "./useRole";

jest.mock("next-auth/react", () => ({ useSession: jest.fn() }));

const mockUseSession = useSession as jest.MockedFunction<typeof useSession>;

function mockSession(roles: string[] | undefined) {
  mockUseSession.mockReturnValue({
    data: roles ? { roles, accessToken: "t", expires: "" } : null,
    status: roles ? "authenticated" : "unauthenticated",
    update: jest.fn(),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
  } as any);
}

describe("useIsAdmin", () => {
  it("returns true when the session has the admin role", () => {
    mockSession(["admin", "customer"]);
    const { result } = renderHook(() => useIsAdmin());
    expect(result.current).toBe(true);
  });

  it("returns false when the session lacks the admin role", () => {
    mockSession(["customer"]);
    const { result } = renderHook(() => useIsAdmin());
    expect(result.current).toBe(false);
  });

  it("returns false when there is no session", () => {
    mockSession(undefined);
    const { result } = renderHook(() => useIsAdmin());
    expect(result.current).toBe(false);
  });
});
