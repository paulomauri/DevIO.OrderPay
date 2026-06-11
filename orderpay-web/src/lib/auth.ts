import KeycloakProvider from "next-auth/providers/keycloak";
import type { NextAuthOptions } from "next-auth";

function extractRoles(accessToken: string): string[] {
  try {
    // JWT payload is base64url — convert to standard base64 before decoding
    const base64 = accessToken.split(".")[1].replace(/-/g, "+").replace(/_/g, "/");
    const payload = JSON.parse(Buffer.from(base64, "base64").toString("utf8"));
    return (payload?.realm_access?.roles as string[]) ?? [];
  } catch {
    return [];
  }
}

export const authOptions: NextAuthOptions = {
  providers: [
    KeycloakProvider({
      clientId:     process.env.KEYCLOAK_CLIENT_ID!,
      clientSecret: process.env.KEYCLOAK_CLIENT_SECRET!,
      issuer:       process.env.KEYCLOAK_ISSUER,
    }),
  ],
  callbacks: {
    async jwt({ token, account }) {
      if (account) {
        token.accessToken  = account.access_token  ?? "";
        token.refreshToken = account.refresh_token ?? "";
        token.expiresAt    = account.expires_at    ?? 0;
        token.roles        = extractRoles(token.accessToken);
      }
      return token;
    },
    async session({ session, token }) {
      session.accessToken = token.accessToken;
      session.roles       = token.roles ?? [];
      return session;
    },
  },
  pages: {
    signIn: "/login",
  },
};
