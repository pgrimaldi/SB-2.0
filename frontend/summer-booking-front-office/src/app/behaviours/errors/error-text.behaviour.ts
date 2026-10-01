import { Injectable, inject, isDevMode } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, combineLatest, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { ApiFieldError, ApiProblem, ApiProblemArgs } from '../../entities/errors/api-problem';

/** Macro group of the translations of the errors that come from the backend. */
const ERROR_GROUP = 'error';
/** Message for a code with no translation yet. */
const UNKNOWN_ERROR = `${ERROR_GROUP}.unknown`;

/**
 * The one place that turns an API error into text for the user. The backend `code` is the
 * translation key under `error.` as it is (`auth.invalid_credentials` → `error.auth.invalid_credentials`),
 * with `args` for its `{{placeholders}}`; a code with no translation yet shows `error.unknown`.
 * The texts follow the language. Show them as text, never as HTML: `args` come from the server.
 */
@Injectable({ providedIn: 'root' })
export class ErrorTextBehaviour {
  private readonly translateService = inject(TranslateService);

  /** Message of the error, e.g. "Credenziali non valide". */
  text(problem: ApiProblem): Observable<string> {
    return this.translate(problem.code, problem.args);
  }

  /**
   * Translation key of the error: `error.<code>`, or `error.unknown` while the code has no
   * translation. For `appI18nText` (with `problem.args` as its params), which keeps the space of the
   * longest translation so that the layout is the same in every language.
   */
  key(problem: ApiProblem): Observable<string> {
    return this.keyOf(problem.code);
  }

  /** Messages of the field errors, by field (the first one when a field has more). */
  fieldTexts(problem: ApiProblem): Observable<Readonly<Record<string, string>>> {
    const errors = (problem.errors ?? []).filter(
      (error, index, all) => all.findIndex(({ field }) => field === error.field) === index,
    );
    if (!errors.length) {
      return of({});
    }
    return combineLatest(
      errors.map((error: ApiFieldError) => this.translate(error.code, error.args)),
    ).pipe(
      map((texts) => Object.fromEntries(errors.map(({ field }, index) => [field, texts[index]]))),
    );
  }

  private translate(code: string, args: ApiProblemArgs | undefined): Observable<string> {
    return this.keyOf(code).pipe(
      switchMap((key) => this.translateService.stream(key, args) as Observable<string>),
    );
  }

  private keyOf(code: string): Observable<string> {
    const key = `${ERROR_GROUP}.${code}`;
    return this.translateService.stream(key).pipe(
      // ngx-translate answers the key itself when it has no translation.
      map((text: string) => {
        if (text !== key) {
          return key;
        }
        if (isDevMode()) {
          // A code the backend sends but we do not translate yet: to add to the translations.
          console.warn(`Missing translation for the error code "${code}" (${key}).`);
        }
        return UNKNOWN_ERROR;
      }),
      distinctUntilChanged(),
    );
  }
}
