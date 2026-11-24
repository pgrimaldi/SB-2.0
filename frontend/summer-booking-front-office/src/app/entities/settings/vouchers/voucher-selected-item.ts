import { VoucherAddItemRequest } from './voucher-add-item-request';

export class VoucherSelectedItem extends VoucherAddItemRequest {
  idVoucher: string | null = null;
  version: string | null = null;
}
