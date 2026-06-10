import KeycloakProvider from "next-auth/providers/keycloak";
import type { NextAuthOptions } from "next-auth";

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
      // On first sign-in, account is populated — store tokens
      if (account) {
        token.accessToken  = account.access_token  ?? "";
        token.refreshToken = account.refresh_token ?? "";
        token.expiresAt    = account.expires_at    ?? 0;
      }
      return token;
    },
    async session({ session, token }) {
      // Forward access token to the client so Axios can attach it to API calls
      session.accessToken = token.accessToken as string;
      return session;
    },
  },
  pages: {
    signIn: "/login",
  },
};
