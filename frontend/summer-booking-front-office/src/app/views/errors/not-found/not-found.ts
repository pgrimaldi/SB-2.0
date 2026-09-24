import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Meta, Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { LanguageService } from '../../../services/i18n/language.service';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './not-found.html',
  styleUrl: './not-found.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFound {
  private readonly languageService = inject(LanguageService);

  protected readonly homeLink = computed(() => `/${this.languageService.current()}/home`);

  constructor() {
    const title = inject(Title);
    const meta = inject(Meta);

    meta.updateTag({ name: 'robots', content: 'noindex' });
    inject(DestroyRef).onDestroy(() => meta.removeTag('name="robots"'));

    inject(TranslateService)
      .stream('not-found.page.title')
      .pipe(takeUntilDestroyed())
      .subscribe((pageTitle: string) => title.setTitle(pageTitle));
  }
}
