import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { combineLatest, switchMap } from 'rxjs';
import { LanguageService } from '../../../../services/i18n/language.service';

/**
 * Renders a translated text and reserves the space of its longest translation,
 * so layouts stay identical in every language. The reserved texts live in hidden
 * pseudo-elements: they are not read by screen readers nor indexed as page content.
 *
 * With `[html]="true"` the translation may contain simple inline markup (e.g. `<em>` for
 * highlighted words); Angular sanitizes it and the reserved space uses the plain text.
 */
@Component({
  selector: '[appI18nText]',
  template: `
    @if (html()) {
      <span class="i18n__text__value" [innerHTML]="text()"></span>
    } @else {
      <span class="i18n__text__value">{{ text() }}</span>
    }
  `,
  styleUrl: './i18n-text.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[attr.data-i18n-reserve-1]': 'reserved()[0] ?? null',
    '[attr.data-i18n-reserve-2]': 'reserved()[1] ?? null',
  },
})
export class I18nText {
  readonly key = input.required<string>({ alias: 'appI18nText' });
  readonly html = input(false);
  /** Interpolation parameters, e.g. `{ year: 2026 }` for `"©{{year}} …"`. */
  readonly params = input<Record<string, unknown>>();

  private readonly translateService = inject(TranslateService);
  private readonly languageService = inject(LanguageService);
  private readonly request$ = toObservable(
    computed(() => ({ key: this.key(), params: this.params() })),
  );

  protected readonly text = toSignal(
    this.request$.pipe(switchMap(({ key, params }) => this.translateService.stream(key, params))),
    { initialValue: '' },
  );

  private readonly translations = toSignal(
    this.request$.pipe(
      switchMap(({ key, params }) =>
        combineLatest(
          this.languageService.languages.map(({ code }) =>
            this.translateService.stream(key, params, code),
          ),
        ),
      ),
    ),
    { initialValue: [] as unknown[] },
  );

  /** The two longest translations different from the visible one (enough for up to three languages). */
  protected readonly reserved = computed(() => {
    const plain = (value: string) => (this.html() ? value.replace(/<[^>]*>/g, '') : value);
    const visible = plain(this.text());
    const key = this.key();

    return this.translations()
      .filter((value): value is string => typeof value === 'string' && value !== key)
      .map(plain)
      .filter((value) => value !== visible)
      .sort((first, second) => second.length - first.length)
      .slice(0, 2);
  });
}
