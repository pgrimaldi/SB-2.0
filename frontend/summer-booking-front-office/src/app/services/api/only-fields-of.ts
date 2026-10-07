/**
 * Only the fields the class declares (each entity class gives every field a starting value): a JSON
 * from the server, or anything else, may carry more at runtime, and types do not strip them.
 */
export function onlyFieldsOf<T extends object>(type: new () => T, value: T): T {
  return Object.fromEntries(
    Object.keys(new type()).map((field) => [field, value[field as keyof T]]),
  ) as T;
}
