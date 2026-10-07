export interface SupportInfo {
  phoneNumber: string;
  mailAddress: string;
  /** One line per period, in the language of the request (`Accept-Language`). */
  supportHour: string[];
}
