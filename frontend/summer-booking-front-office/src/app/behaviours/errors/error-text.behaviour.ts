import { Injectable, inject, isDevMode } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { Observable, combineLatest, distinctUntilChanged, map, of, switchMap } from 'rxjs';
import { ApiFieldError, ApiProblem, ApiProblemArgs } from '../../entities/errors/api-problem';

const ERROR_GROUP = 'error';
const UNKNOWN_ERROR = `${ERROR_GROUP}.unknown`;

/**
 * The one place that turns an API error into text for the user. The backend `code` is the
 * translation key under `error.` as it is (`auth.invalid_credentials` → `error.auth.invalid_credentials`),
 * with `args` for its placeholders (`{{name}}`, or typed ones such as `{date, date, long}` of the
 * message-format library); a code with no translation yet shows `error.unknown`.
 * The texts follow the language. Show them as text, never as HTML: `args` come from the server.
 */
@Injectable({ providedIn: 'root' })
export class ErrorTextBehaviour {
  private readonly translateService = inject(TranslateService);

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

  /** The text now, for a `computed` that follows the language by itself (e.g. form field errors). */
  instant(code: string, args?: ApiProblemArgs): string {
    const key = `${ERROR_GROUP}.${code}`;
    const text = this.translateService.instant(key, args) as string;
    // ngx-translate answers the key itself when it has no translation.
    return text === key ? (this.translateService.instant(UNKNOWN_ERROR) as string) : text;
  }

  /** Only the first error of each field. */
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
          console.warn(`Missing translation for the error code "${code}" (${key}).`);
        }
        return UNKNOWN_ERROR;
      }),
      distinctUntilChanged(),
    );
  }
}
