import { PackageAddItemRequest } from './package-add-item-request';

export class PackageSelectedItem extends PackageAddItemRequest {
  idPackage: string | null = null;
}
