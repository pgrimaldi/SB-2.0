export interface BulkFailure {
  id: string;
  code: string;
}

/** What an action on more elements did: those it could, and why not the others. */
export interface BulkResult {
  succeeded: string[];
  failed: BulkFailure[];
}
