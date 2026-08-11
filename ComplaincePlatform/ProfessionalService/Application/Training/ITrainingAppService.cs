namespace ProfessionalService.Application.Training;

public interface ITrainingAppService
{
    Task<IReadOnlyList<TrainingProgramDto>> ListProgramsAsync(Guid? organizationId, bool? mandatoryOnly, CancellationToken ct = default);
    Task<TrainingProgramDto?> GetProgramAsync(Guid id, CancellationToken ct = default);
    Task<TrainingProgramDto> CreateProgramAsync(CreateTrainingProgramRequest request, CancellationToken ct = default);
    Task<TrainingProgramDto?> UpdateProgramAsync(Guid id, UpdateTrainingProgramRequest request, CancellationToken ct = default);
    Task<bool> DeleteProgramAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<TrainingSessionDto>> ListSessionsAsync(Guid? programId, DateTime? from, DateTime? to, CancellationToken ct = default);
    Task<TrainingSessionDto?> GetSessionAsync(Guid id, CancellationToken ct = default);
    Task<TrainingSessionDto> CreateSessionAsync(CreateTrainingSessionRequest request, CancellationToken ct = default);
    Task<TrainingSessionDto?> UpdateSessionAsync(Guid id, UpdateTrainingSessionRequest request, CancellationToken ct = default);
    Task<bool> DeleteSessionAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<TrainingAttendanceDto>> AssignParticipantsAsync(Guid sessionId, AssignParticipantsRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TrainingAttendanceDto>> ListAttendancesAsync(Guid sessionId, CancellationToken ct = default);
    Task<TrainingAttendanceDto?> RecordAttendanceAsync(Guid attendanceId, RecordAttendanceRequest request, CancellationToken ct = default);
    Task<TrainingAssessmentDto?> RecordAssessmentAsync(Guid attendanceId, RecordAssessmentRequest request, CancellationToken ct = default);
    Task<TrainingAttendanceDto?> SetCertificateUrlAsync(Guid attendanceId, SetCertificateUrlRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<TrainingMatrixItemDto>> GetMatrixAsync(Guid? organizationId, string? roleCode, Guid? departmentId, CancellationToken ct = default);
}
