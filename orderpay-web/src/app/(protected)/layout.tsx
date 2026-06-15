import AppShell from "@/components/layout/AppShell";
import ErrorBoundary from "@/components/ui/ErrorBoundary";

export default function ProtectedLayout({ children }: { children: React.ReactNode }) {
  return (
    <ErrorBoundary>
      <AppShell>{children}</AppShell>
    </ErrorBoundary>
  );
}
