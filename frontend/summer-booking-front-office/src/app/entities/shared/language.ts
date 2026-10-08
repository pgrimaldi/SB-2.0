/** The values are the codes in the URL (`/it/home`), in the translation file names and in ngx-translate. */
export enum Language {
  It = 'it',
  En = 'en',
}

export interface LanguageOption {
  code: Language;
  flagSrc: string;
}
