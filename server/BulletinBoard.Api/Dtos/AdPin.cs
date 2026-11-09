using BulletinBoard.Api.Models;

namespace BulletinBoard.Api.Dtos;

/// <summary>The light shape the map needs for one pin.</summary>
public sealed record AdPin(Guid Id, string Title, Category Category, double Latitude, double Longitude);
