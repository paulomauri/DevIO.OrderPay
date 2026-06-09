const theme = {
  colors: {
    primary:        "#2563EB",
    primaryHover:   "#1D4ED8",
    secondary:      "#64748B",
    background:     "#F8FAFC",
    surface:        "#FFFFFF",
    border:         "#E2E8F0",
    text:           "#0F172A",
    textMuted:      "#64748B",
    error:          "#DC2626",
    success:        "#16A34A",
    warning:        "#D97706",

    // order status badge colors
    status: {
      pending:               "#F59E0B",
      awaitingPayment:       "#3B82F6",
      paymentConfirmed:      "#10B981",
      processing:            "#8B5CF6",
      shipped:               "#06B6D4",
      delivered:             "#16A34A",
      cancellationRequested: "#F97316",
      refunding:             "#EF4444",
      cancelled:             "#6B7280",
    },
  },

  spacing: {
    xs:  "4px",
    sm:  "8px",
    md:  "16px",
    lg:  "24px",
    xl:  "32px",
    xxl: "48px",
  },

  typography: {
    fontFamily: "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif",
    fontSize: {
      xs:  "12px",
      sm:  "14px",
      md:  "16px",
      lg:  "18px",
      xl:  "24px",
      xxl: "32px",
    },
    fontWeight: {
      regular:  400,
      medium:   500,
      semibold: 600,
      bold:     700,
    },
    lineHeight: {
      tight:  "1.25",
      normal: "1.5",
      loose:  "1.75",
    },
  },

  borderRadius: {
    sm: "4px",
    md: "8px",
    lg: "12px",
    xl: "16px",
    full: "9999px",
  },

  shadows: {
    sm: "0 1px 2px rgba(0, 0, 0, 0.05)",
    md: "0 4px 6px rgba(0, 0, 0, 0.07)",
    lg: "0 10px 15px rgba(0, 0, 0, 0.10)",
  },

  breakpoints: {
    sm:  "640px",
    md:  "768px",
    lg:  "1024px",
    xl:  "1280px",
    xxl: "1536px",
  },

  sidebar: {
    width: "240px",
  },
} as const;

export type Theme = typeof theme;
export default theme;
