import { httpClient } from './httpClient'
import type {
  ApplicationResponse,
  ApplicationScheduleResponse,
  ApplicationsPageResponse,
  ApproveApplicationRequest,
  CreateApplicationRequest,
  EmployeeApplicationDetailsResponse,
  EmployeeApplicationsPageResponse,
  EmployeeApplicationsQuery,
  RejectApplicationRequest,
} from './applications.types'

export const applicationsApi = {
  async getPage(page = 1, pageSize = 10): Promise<ApplicationsPageResponse> {
    const { data } = await httpClient.get<ApplicationsPageResponse>(
      '/applications',
      { params: { page, pageSize } },
    )
    return data
  },

  async getById(id: string): Promise<ApplicationResponse> {
    const { data } = await httpClient.get<ApplicationResponse>(
      `/applications/${id}`,
    )
    return data
  },

  async getSchedule(id: string): Promise<ApplicationScheduleResponse> {
    const { data } = await httpClient.get<ApplicationScheduleResponse>(
      `/applications/${id}/schedule`,
    )
    return data
  },

  async create(
    request: CreateApplicationRequest,
  ): Promise<ApplicationResponse> {
    const { data } = await httpClient.post<ApplicationResponse>(
      '/applications',
      request,
      { headers: { 'Idempotency-Key': crypto.randomUUID() } },
    )
    return data
  },

  async withdraw(id: string): Promise<ApplicationResponse> {
    const { data } = await httpClient.post<ApplicationResponse>(
      `/applications/${id}/withdraw`,
    )
    return data
  },

  async getEmployeePage(
    query: EmployeeApplicationsQuery,
    signal?: AbortSignal,
  ): Promise<EmployeeApplicationsPageResponse> {
    const { data } = await httpClient.get<EmployeeApplicationsPageResponse>(
      '/employee/applications',
      { params: query, signal },
    )
    return data
  },

  async getEmployeeById(
    id: string,
    signal?: AbortSignal,
  ): Promise<EmployeeApplicationDetailsResponse> {
    const { data } = await httpClient.get<EmployeeApplicationDetailsResponse>(
      `/employee/applications/${id}`,
      { signal },
    )
    return data
  },

  async approveEmployeeApplication(
    id: string,
    request: ApproveApplicationRequest,
  ): Promise<ApplicationResponse> {
    const { data } = await httpClient.post<ApplicationResponse>(
      `/employee/applications/${id}/approve`,
      request,
    )
    return data
  },

  async rejectEmployeeApplication(
    id: string,
    request: RejectApplicationRequest,
  ): Promise<ApplicationResponse> {
    const { data } = await httpClient.post<ApplicationResponse>(
      `/employee/applications/${id}/reject`,
      request,
    )
    return data
  },
}
