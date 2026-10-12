import { httpClient } from './httpClient'
import type { CreditProductResponse } from './products.types'

export const productsApi = {
  async getActive(): Promise<CreditProductResponse[]> {
    const { data } = await httpClient.get<CreditProductResponse[]>('/products')
    return data
  },
}
