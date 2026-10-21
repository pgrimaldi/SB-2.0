// Published DEV environment: optimized build like production, with the mock API switched on.
export const environment = {
  production: true,
  apiBaseUrl: '/api',
  authApp: 'hub',
  stripeClientId: 'ca_JisDtZSHRochs17qUa9Cd9QSd6jzP4yX',
  stripeConnectReturnUrl: 'https://devhub.summerbooking.it/settings',
  googleMapsApiKey: 'AIzaSyBmO7JhJqXB2Tgj3uLmmBUsIMZu80bfp7o',
  googleMapsMapId: 'DEMO_MAP_ID'
} as const;