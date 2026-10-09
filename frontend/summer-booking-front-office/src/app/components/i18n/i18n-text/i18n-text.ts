import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';
import { combineLatest, switchMap } from 'rxjs';

/**
 * Renders a translated text and reserves the space of its longest translation,
 * so layouts stay identical in every language. The reserved texts live in hidden
 * pseudo-elements: they are not indexed as page content.
 *
 * With `[isHtml]="true"` the translation may contain simple inline markup (e.g. `<em>` for
 * highlighted words); Angular sanitizes it and the reserved space uses the plain text.
 * The languages are the ones ngx-translate knows (`addLangs`, or loaded translations): the
 * directive needs nothing else from the app.
 */
@Component({
  selector: '[appI18nText]',
  template: `
    @if (isHtml()) {
      <span class="i18n__text__value" [innerHTML]="text()"></span>
    } @else {
      <span class="i18n__text__value">{{ text() }}</span>
    }
    @for (translation of reserved(); track translation) {
      <span class="i18n__text__reserve" [attr.data-i18n-reserve]="translation"></span>
    }
  `,
  styleUrl: './i18n-text.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class I18nText {
  readonly key = input.required<string>({ alias: 'appI18nText' });
  readonly isHtml = input(false);
  readonly params = input<Record<string, unknown>>();

  private readonly translateService = inject(TranslateService);
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
          this.translateService
            .getLangs()
            .map((language) => this.translateService.stream(key, params, language)),
        ),
      ),
    ),
    { initialValue: [] as unknown[] },
  );

  protected readonly reserved = computed(() => {
    const plain = (value: string) => (this.isHtml() ? value.replace(/<[^>]*>/g, '') : value);
    const visible = plain(this.text());
    const key = this.key();

    return [
      ...new Set(
        this.translations()
          .filter((value): value is string => typeof value === 'string' && value !== key)
          .map(plain)
          .filter((value) => value !== visible),
      ),
    ];
  });
}
