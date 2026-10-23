import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { LanguageBehaviour } from '../i18n/language.behaviour';
import { GoogleMapsBehaviour } from './google-maps.behaviour';

describe('GoogleMapsBehaviour', () => {
  it('should reject without contacting Google when no build-time key is configured', async () => {
    const createElement = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        { provide: DOCUMENT, useValue: { createElement } },
        { provide: LanguageBehaviour, useValue: {} },
      ],
    });

    await expect(TestBed.inject(GoogleMapsBehaviour).load()).rejects.toThrow(
      'Google Maps API key not configured',
    );
    expect(createElement).not.toHaveBeenCalled();
  });
});
