using Shared.Contracts.Events;

namespace Shared.Contracts.Identity;

/// <summary>Nuovo cliente registrato: Notification registra il consenso newsletter, se dato.</summary>
public sealed record AccountRegistered(
	Guid EventId, DateTime OccurredAt, Guid UserId, string Email, string Language,
	bool NewsletterConsent, string? ConsentNoticeVersion) : IIntegrationEvent;

/// <summary>Account chiuso: gli altri moduli scollegano i propri dati.</summary>
public sealed record AccountDeleted(Guid EventId, DateTime OccurredAt, Guid UserId) : IIntegrationEvent;

/// <summary>Spunte «email/cellulare come contatto» di un utente staff (notification_user_channel_preferences).</summary>
public sealed record StaffContactChannelsChanged(
	Guid EventId, DateTime OccurredAt, Guid UserId, Guid PropertyId,
	bool EmailIsContact, bool PhoneIsContact) : IIntegrationEvent;

/// <summary>Titolare assegnato a uno stabilimento (catalog: proprietario della property).</summary>
public sealed record PropertyOwnerAssigned(Guid EventId, DateTime OccurredAt, Guid PropertyId, Guid OwnerUserId) : IIntegrationEvent;

/// <summary>Il systemadmin abilita o disabilita la prenotazione online dello stabilimento.</summary>
public sealed record PropertyOnlineReservationSet(Guid EventId, DateTime OccurredAt, Guid PropertyId, bool Enabled) : IIntegrationEvent;

/// <summary>Cambio di stato dell'abbonamento: catalog aggiorna la disponibilità online.</summary>
public sealed record PropertySubscriptionChanged(
	Guid EventId, DateTime OccurredAt, Guid PropertyId, string Status, string LicensePlanCode, DateTime EndsAt) : IIntegrationEvent;

/// <summary>"Mi piace" aggiunto o tolto: catalog ricalcola il contatore della struttura.</summary>
public sealed record BathhouseLikeChanged(Guid EventId, DateTime OccurredAt, Guid BathhouseId, Guid UserId, bool Liked) : IIntegrationEvent;
