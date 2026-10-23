import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Language } from '../../entities/shared/language';
import { LanguageBehaviour } from '../i18n/language.behaviour';

const SCRIPT_URL = 'https://maps.googleapis.com/maps/api/js';
/** Called by Google when the API is ready: the end of the script's download comes earlier. */
const READY_CALLBACK = 'sbGoogleMapsReady';
/** The languages of the app that Google names differently. */
const GOOGLE_LANGUAGES: Partial<Record<Language, string>> = { [Language.Zh]: 'zh-CN' };

type GoogleMapsWindow = Window & { [READY_CALLBACK]?: () => void };

@Injectable({ providedIn: 'root' })
export class GoogleMapsBehaviour {
  private readonly document = inject(DOCUMENT);
  private readonly language = inject(LanguageBehaviour);

  private loading: Promise<void> | null = null;

  load(): Promise<void> {
    if (!environment.googleMapsApiKey) {
      return Promise.reject(new Error('Google Maps API key not configured'));
    }

    this.loading ??= new Promise<void>((resolve, reject) => {
      const view = this.document.defaultView as GoogleMapsWindow;
      const language = this.language.current();
      const parameters = new URLSearchParams({
        key: environment.googleMapsApiKey,
        loading: 'async',
        callback: READY_CALLBACK,
        language: GOOGLE_LANGUAGES[language] ?? language,
      });
      const script = this.document.createElement('script');
      script.src = `${SCRIPT_URL}?${parameters}`;
      script.async = true;
      view[READY_CALLBACK] = () => {
        delete view[READY_CALLBACK];
        resolve();
      };
      script.onerror = () => {
        delete view[READY_CALLBACK];
        script.remove();
        this.loading = null;
        reject(new Error('Google Maps JavaScript API not loaded'));
      };
      this.document.head.append(script);
    });
    return this.loading;
  }
}
