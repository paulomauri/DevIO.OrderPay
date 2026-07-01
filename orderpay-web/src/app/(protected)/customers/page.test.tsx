import { renderWithProviders, screen } from "@/test/test-utils";
import CustomersPage from "./page";
import { customersService } from "@/services/customers";
import { useSession } from "next-auth/react";
import type { CustomerResponse } from "@/types/customer";

jest.mock("@/services/customers", () => ({
  customersService: { getAll: jest.fn() },
}));
jest.mock("next-auth/react", () => ({ useSession: jest.fn() }));

const mockGetAll = customersService.getAll as jest.MockedFunction<
  typeof customersService.getAll
>;
const mockUseSession = useSession as jest.MockedFunction<typeof useSession>;

function setRoles(roles: string[]) {
  mockUseSession.mockReturnValue({
    data: { roles, accessToken: "t", expires: "" },
    status: "authenticated",
    update: jest.fn(),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
  } as any);
}

const customers: CustomerResponse[] = [
  { id: "1", name: "Ada Lovelace", email: "ada@example.com", cpf: "11111111111", mobile: "+55 11 90000-0001" },
  { id: "2", name: "Alan Turing",  email: "alan@example.com", cpf: "22222222222", mobile: "+55 11 90000-0002" },
];

describe("CustomersPage", () => {
  it("renders a table row for each customer", async () => {
    setRoles(["admin"]);
    mockGetAll.mockResolvedValue(customers);

    renderWithProviders(<CustomersPage />);

    expect(await screen.findByText("Ada Lovelace")).toBeInTheDocument();
    expect(screen.getByText("Alan Turing")).toBeInTheDocument();
    expect(screen.getByText("ada@example.com")).toBeInTheDocument();
  });

  it("shows an empty state when there are no customers", async () => {
    setRoles(["admin"]);
    mockGetAll.mockResolvedValue([]);

    renderWithProviders(<CustomersPage />);

    expect(await screen.findByText("No customers found.")).toBeInTheDocument();
  });

  it("shows admin actions for admins", async () => {
    setRoles(["admin"]);
    mockGetAll.mockResolvedValue(customers);

    renderWithProviders(<CustomersPage />);

    await screen.findByText("Ada Lovelace");
    expect(screen.getByRole("button", { name: "+ New Customer" })).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: "Delete" }).length).toBe(2);
  });

  it("hides admin actions for non-admins", async () => {
    setRoles(["customer"]);
    mockGetAll.mockResolvedValue(customers);

    renderWithProviders(<CustomersPage />);

    await screen.findByText("Ada Lovelace");
    expect(screen.queryByRole("button", { name: "+ New Customer" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete" })).not.toBeInTheDocument();
  });
});
