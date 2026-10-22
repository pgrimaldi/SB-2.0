import { googleMapsApiKey } from './google-maps-api-key';

// Local development (mock API: see services/mocks/mock-interceptors.ts).
export const environment = {
  production: false,
  apiBaseUrl: '/api',
  authApp: 'hub',
  stripeClientId: 'ca_JisDtZSHRochs17qUa9Cd9QSd6jzP4yX',
  stripeConnectReturnUrl: 'https://devhub.summerbooking.it/settings',
  googleMapsApiKey,
  googleMapsMapId: 'DEMO_MAP_ID',
} as const;
