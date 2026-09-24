import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { I18nText } from '../../shared/i18n/i18n-text/i18n-text';

interface FooterLink {
  key: string;
  highlight?: boolean;
}

@Component({
  selector: 'app-hub-footer',
  imports: [I18nText, TranslatePipe],
  templateUrl: './footer.html',
  styleUrl: './footer.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HubFooter {
  protected readonly socials = ['instagram', 'facebook', 'email', 'youtube'] as const;

  protected readonly columns: readonly (readonly FooterLink[])[] = [
    [{ key: 'about' }, { key: 'support' }, { key: 'demo', highlight: true }],
    [{ key: 'contacts' }, { key: 'legal' }],
    [{ key: 'terms' }, { key: 'privacy' }, { key: 'cookies' }],
  ];

  protected readonly year = { year: new Date().getFullYear() };
}
