using HighFidelity.Api.Models;
using HighFidelity.Api.DTOs;

namespace HighFidelity.Api.Mappings;

/// <summary>
/// Manual mapping extensions between domain entities and API DTOs.
/// No AutoMapper — explicit mapping is easier to debug, test, and refactor.
/// The properties are simple enough that AutoMapper adds ceremony without value.
/// </summary>
public static class EntityMappings
{
    public static DashboardCardDto ToDto(this DashboardCard entity) =>
        new(entity.Id, entity.Title, entity.Amount, entity.AmountDisplay, entity.Icon, entity.ThemeColorHex);

    public static RevenueCardDto ToDto(this RevenueCard entity) =>
        new(entity.Id, entity.Title, entity.Value, entity.Subtitle, entity.ChartType, entity.BackgroundHex, entity.AccentHex);

    public static ActivityDto ToDto(this Activity entity) =>
        new(entity.Id, entity.Title, entity.Actor, entity.Action, entity.Time, entity.Icon, entity.IconColorHex);

    public static OrderDto ToDto(this Order entity) =>
        new(entity.Id, entity.Invoice, entity.Customer, entity.Country, entity.Price, entity.Status);

    public static TrafficDto ToDto(this TrafficSource entity) =>
        new(entity.Id, entity.Source, entity.Percentage, entity.SegmentColorHex);
}
