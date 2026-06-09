import type { Metadata } from "next";
import Providers from "@/lib/providers";

export const metadata: Metadata = {
  title: "OrderPay",
  description: "Order management platform",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="en">
      <body>
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
