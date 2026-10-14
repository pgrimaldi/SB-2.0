import { DiscountType } from '../../enums/discount-type';

export class VoucherItem {
  idVoucher: string | null = null;
  voucherName: string | null = null;
  validFrom: string | null = null;
  validTo: string | null = null;
  discountType: DiscountType | null = null;
  discountValue: number | null = null;
}
