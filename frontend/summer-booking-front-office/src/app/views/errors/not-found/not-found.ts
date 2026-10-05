import { ChangeDetectionStrategy, Component, OnDestroy, computed, inject } from '@angular/core';
import { Meta, Title } from '@angular/platform-browser';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';
import { LanguageBehaviour } from '../../../behaviours/i18n/language.behaviour';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink, TranslatePipe],
  templateUrl: './not-found.html',
  styleUrl: './not-found.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFound implements OnDestroy {
  private readonly languageBehaviour = inject(LanguageBehaviour);
  private readonly translateService = inject(TranslateService);
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  protected readonly homeLink = computed(() => `/${this.languageBehaviour.current()}/home`);
  private readonly titleSubscription: Subscription;

  constructor() {
    // A missing page must not be indexed.
    this.meta.updateTag({ name: 'robots', content: 'noindex' });
    this.titleSubscription = this.translateService
      .stream('not_found.page.title')
      .subscribe((pageTitle: string) => this.showTitle(pageTitle));
  }

  private showTitle(pageTitle: string): void {
    this.title.setTitle(pageTitle);
  }

  ngOnDestroy(): void {
    this.titleSubscription.unsubscribe();
    this.meta.removeTag('name="robots"');
  }
}
