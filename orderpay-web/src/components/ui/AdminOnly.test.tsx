import { render, screen } from "@testing-library/react";
import AdminOnly from "./AdminOnly";
import { useIsAdmin } from "@/hooks/useRole";

jest.mock("@/hooks/useRole", () => ({ useIsAdmin: jest.fn() }));

const mockUseIsAdmin = useIsAdmin as jest.MockedFunction<typeof useIsAdmin>;

describe("AdminOnly", () => {
  it("renders children when the user is an admin", () => {
    mockUseIsAdmin.mockReturnValue(true);
    render(<AdminOnly><button>Delete</button></AdminOnly>);
    expect(screen.getByRole("button", { name: "Delete" })).toBeInTheDocument();
  });

  it("renders nothing when the user is not an admin", () => {
    mockUseIsAdmin.mockReturnValue(false);
    render(<AdminOnly><button>Delete</button></AdminOnly>);
    expect(screen.queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
  });

  it("renders the fallback when provided and the user is not an admin", () => {
    mockUseIsAdmin.mockReturnValue(false);
    render(
      <AdminOnly fallback={<span>read only</span>}>
        <button>Delete</button>
      </AdminOnly>
    );
    expect(screen.getByText("read only")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
  });
});
