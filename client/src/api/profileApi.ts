import { httpClient } from './httpClient'
import type { ProfileResponse, UpdateProfileRequest } from './profile.types'

export const profileApi = {
  async get(): Promise<ProfileResponse> {
    const { data } = await httpClient.get<ProfileResponse>('/profile')
    return data
  },

  async update(request: UpdateProfileRequest): Promise<ProfileResponse> {
    const { data } = await httpClient.put<ProfileResponse>('/profile', request)
    return data
  },
}
