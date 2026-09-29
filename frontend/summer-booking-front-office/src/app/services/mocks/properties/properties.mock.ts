/**
 * Rows of `catalog.properties` for the mocks: same structure as the sample DB, invented values
 * (no data of real properties). `id` is the internal key other tables refer to (`property_id`),
 * `publicId` the one the API exposes (`idProperty`).
 */
export const MOCK_PROPERTIES = [
  { id: 1, publicId: '7ddc54a7-233b-4f4b-a365-68be583e661f', name: 'Lido Demo' },
  { id: 2, publicId: 'f9ecf8d2-02b7-4606-adbb-e560486ed90a', name: 'Bagni Esempio' },
] as const;

/** Property managed by the mock test account. */
export const DEMO_PROPERTY = MOCK_PROPERTIES[0];
