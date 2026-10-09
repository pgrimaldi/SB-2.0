/** The values are the codes in the URL (`/it/home`), in the translation file names and in ngx-translate. */
export enum Language {
  It = 'it',
  En = 'en',
  Fr = 'fr',
  Es = 'es',
  De = 'de',
  Zh = 'zh',
  Ar = 'ar',
}

export interface LanguageOption {
  code: Language;
  flagSrc: string;
  /** Written from right to left (Arabic): the whole page is mirrored. */
  isRightToLeft: boolean;
}
