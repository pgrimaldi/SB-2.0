// Published DEV environment: optimized build like production, with the mock API switched on.
export const environment = {
  production: true,
  apiBaseUrl: '/api',
  features: {
    useMocks: true,
  },
} as const;
