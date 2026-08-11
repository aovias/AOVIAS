using CommunicationService.Application.Contracts;
using MediatR;

namespace CommunicationService.Application.Commands;

public sealed record SendCommunicationCommand(SendCommunicationRequest Request) : IRequest<SendCommunicationResult>;
public sealed record MarkNotificationReadCommand(Guid PersonId, Guid NotificationId) : IRequest;
public sealed record MarkAllNotificationsReadCommand(Guid PersonId) : IRequest;
public sealed record UpdatePreferenceCommand(Guid PersonId, UpdatePreferenceRequest Request) : IRequest<PreferenceDto>;
public sealed record UpsertTemplateCommand(UpsertTemplateRequest Request) : IRequest<TemplateDto>;
public sealed record CreateReminderRuleCommand(CreateReminderRuleRequest Request) : IRequest<ReminderRuleDto>;
public sealed record CreateCampaignCommand(CreateCampaignRequest Request) : IRequest<CampaignDto>;
public sealed record LaunchCampaignCommand(Guid CampaignId) : IRequest;
public sealed record UpdateAvailabilityCommand(Guid PersonId, UpdateAvailabilityRequest Request) : IRequest<AvailabilityDto>;
