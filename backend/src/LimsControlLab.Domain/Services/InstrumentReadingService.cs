using LimsControlLab.Domain.Auth;
using LimsControlLab.Domain.Common;
using LimsControlLab.Domain.Entities;
using LimsControlLab.Domain.Repositories;
using LimsControlLab.SharedKernel.Enums;

namespace LimsControlLab.Domain.Services;

public sealed class InstrumentReadingService
{
    private readonly IInstrumentRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public InstrumentReadingService(
        IInstrumentRepository repository,
        ICurrentUser currentUser,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<Outcome<List<InstrumentDto>>> ListByCurrentSiteAsync(CancellationToken ct)
    {
        var instruments = await _repository.ListBySiteAsync(_currentUser.Site, ct);
        var dtos = instruments.Select(i => MapToDto(i)).ToList();
        return new Outcome<List<InstrumentDto>>.Ok(dtos);
    }

    public async Task<Outcome<InstrumentDto>> GetByIdAsync(int id, CancellationToken ct)
    {
        var instrument = await _repository.GetByIdAsync(id, ct);
        if (instrument == null)
            return new Outcome<InstrumentDto>.NotFound($"Instrument {id} not found.");

        if (instrument.Site != _currentUser.Site)
            return new Outcome<InstrumentDto>.Forbidden("You can only access instruments for your own site.");

        return new Outcome<InstrumentDto>.Ok(MapToDto(instrument));
    }

    public async Task<Outcome<InstrumentDto>> CreateAsync(CreateInstrumentRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(request.Name))
            return new Outcome<InstrumentDto>.Invalid("name", "Instrument name is required.");

        if (_currentUser.Role != Role.LabCoordinator)
            return new Outcome<InstrumentDto>.Forbidden("Only Lab Coordinators can create instruments.");

        var instrument = new Instrument
        {
            Name = request.Name,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            Site = _currentUser.Site,
            IsActive = request.IsActive,
        };

        await _repository.AddAsync(instrument, ct);

        return new Outcome<InstrumentDto>.Ok(MapToDto(instrument));
    }

    public async Task<Outcome<InstrumentDto>> UpdateAsync(int id, UpdateInstrumentRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrEmpty(request.Name))
            return new Outcome<InstrumentDto>.Invalid("name", "Instrument name is required.");

        if (string.IsNullOrEmpty(request.RowVersion))
            return new Outcome<InstrumentDto>.Invalid("rowVersion", "Row version is required for concurrency control.");

        if (_currentUser.Role != Role.LabCoordinator)
            return new Outcome<InstrumentDto>.Forbidden("Only Lab Coordinators can update instruments.");

        var instrument = await _repository.GetByIdAsync(id, ct);
        if (instrument == null)
            return new Outcome<InstrumentDto>.NotFound($"Instrument {id} not found.");

        if (instrument.Site != _currentUser.Site)
            return new Outcome<InstrumentDto>.Forbidden("You can only update instruments for your own site.");

        instrument.Name = request.Name;
        instrument.Model = request.Model;
        instrument.SerialNumber = request.SerialNumber;
        instrument.IsActive = request.IsActive;

        var expectedRowVersion = Convert.FromBase64String(request.RowVersion);
        var concurrencySuccess = await _repository.TryUpdateWithConcurrencyCheckAsync(instrument, expectedRowVersion, ct);

        if (!concurrencySuccess)
        {
            var refreshed = await _repository.GetByIdAsync(id, ct);
            return new Outcome<InstrumentDto>.Conflict(
                $"Concurrent modification detected. Current row version is {Convert.ToBase64String(refreshed!.RowVersion)}");
        }

        var updated = await _repository.GetByIdAsync(id, ct);
        return new Outcome<InstrumentDto>.Ok(MapToDto(updated!));
    }

    private static InstrumentDto MapToDto(Instrument instrument) => new()
    {
        Id = instrument.Id,
        Name = instrument.Name,
        Model = instrument.Model,
        SerialNumber = instrument.SerialNumber,
        Site = instrument.Site.ToString(),
        IsActive = instrument.IsActive,
        RowVersion = Convert.ToBase64String(instrument.RowVersion),
    };
}

public sealed record CreateInstrumentRequest
{
    public required string Name { get; init; }
    public string? Model { get; init; }
    public string? SerialNumber { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record UpdateInstrumentRequest
{
    public required string Name { get; init; }
    public string? Model { get; init; }
    public string? SerialNumber { get; init; }
    public required bool IsActive { get; init; }
    public required string RowVersion { get; init; }
}

public sealed record InstrumentDto
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? Model { get; init; }
    public string? SerialNumber { get; init; }
    public required string Site { get; init; }
    public required bool IsActive { get; init; }
    public required string RowVersion { get; init; }
}
