import userEvent from "@testing-library/user-event";
import { renderWithProviders, screen } from "@/test/test-utils";
import Button from "./Button";

describe("Button", () => {
  it("renders its children", () => {
    renderWithProviders(<Button>Save</Button>);
    expect(screen.getByRole("button", { name: "Save" })).toBeInTheDocument();
  });

  it("shows a spinner while loading", () => {
    renderWithProviders(<Button loading>Save</Button>);
    expect(screen.getByLabelText("Loading")).toBeInTheDocument();
  });

  it("is disabled while loading", () => {
    renderWithProviders(<Button loading>Save</Button>);
    expect(screen.getByRole("button")).toBeDisabled();
  });

  it("is disabled when disabled prop is set", () => {
    renderWithProviders(<Button disabled>Save</Button>);
    expect(screen.getByRole("button", { name: "Save" })).toBeDisabled();
  });

  it("does not fire onClick while loading", async () => {
    const onClick = jest.fn();
    renderWithProviders(<Button loading onClick={onClick}>Save</Button>);
    await userEvent.click(screen.getByRole("button"));
    expect(onClick).not.toHaveBeenCalled();
  });

  it("fires onClick when enabled", async () => {
    const onClick = jest.fn();
    renderWithProviders(<Button onClick={onClick}>Save</Button>);
    await userEvent.click(screen.getByRole("button", { name: "Save" }));
    expect(onClick).toHaveBeenCalledTimes(1);
  });

  it.each(["primary", "secondary", "danger", "ghost"] as const)(
    "renders the %s variant",
    (variant) => {
      renderWithProviders(<Button variant={variant}>Save</Button>);
      expect(screen.getByRole("button", { name: "Save" })).toBeInTheDocument();
    }
  );
});
