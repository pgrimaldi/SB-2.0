import { DiscountScope } from '../../enums/discount-scope';
import { DiscountType } from '../../enums/discount-type';
import { VoucherValidityPeriod } from './voucher-validity-period';

export class VoucherSelectedItem {
  idVoucher: string | null = null;
  voucherName: string | null = null;
  validityPeriods: VoucherValidityPeriod[] = [];
  discountType: DiscountType | null = null;
  discountValue: number | null = null;
  discountScope: DiscountScope | null = null;
  maxUseQuantity: number | null = null;
  isEnabled: boolean | null = null;
}
