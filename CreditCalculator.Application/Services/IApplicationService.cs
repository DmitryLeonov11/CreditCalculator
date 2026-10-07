using CreditCalculator.Application.Contracts;

namespace CreditCalculator.Application.Services;

public interface IApplicationService
{
    Task<CreateApplicationResult> CreateAsync(
        Guid userId,
        CreateApplicationRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<ApplicationResponse>> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResponse<EmployeeApplicationListItemResponse>> GetEmployeePagedAsync(
        EmployeeApplicationsQuery query,
        CancellationToken cancellationToken = default);

    Task<EmployeeApplicationDetailsResponse> GetEmployeeByIdAsync(
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<EmployeeStatisticsResponse> GetEmployeeStatisticsAsync(
        DateTimeOffset? createdFrom,
        DateTimeOffset? createdTo,
        CancellationToken cancellationToken = default);

    Task<ApplicationResponse> GetByIdAsync(
        Guid userId,
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<ApplicationScheduleResponse> GetScheduleAsync(
        Guid userId,
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResponse> WithdrawAsync(
        Guid userId,
        Guid applicationId,
        CancellationToken cancellationToken = default);

    Task<ApplicationResponse> ApproveAsync(
        Guid employeeId,
        Guid applicationId,
        ApproveApplicationRequest request,
        CancellationToken cancellationToken = default);

    Task<ApplicationResponse> RejectAsync(
        Guid employeeId,
        Guid applicationId,
        RejectApplicationRequest request,
        CancellationToken cancellationToken = default);
}
