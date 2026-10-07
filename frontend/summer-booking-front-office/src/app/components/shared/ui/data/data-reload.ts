import { InjectionToken, Signal } from '@angular/core';

/**
 * What the data of the server depends on besides the request itself: in this app the language
 * (`Accept-Language`), because the server may answer with translated data. When it changes, what was
 * loaded is loaded again: components that load data by themselves (e.g. `app-table`, which keeps
 * page, search and sorting) read it, and so must any other load of server data. Optional: without it
 * nothing reloads.
 */
export const DATA_RELOAD = new InjectionToken<Signal<unknown>>('DATA_RELOAD');
