"use client";

import { useIsAdmin } from "@/hooks/useRole";

interface AdminOnlyProps {
  children:  React.ReactNode;
  fallback?: React.ReactNode;
}

/**
 * Renders children only when the current user has the Keycloak "admin" role.
 * Backend still enforces authorization — this is for UX only.
 */
export default function AdminOnly({ children, fallback = null }: AdminOnlyProps) {
  const isAdmin = useIsAdmin();
  return isAdmin ? <>{children}</> : <>{fallback}</>;
}
