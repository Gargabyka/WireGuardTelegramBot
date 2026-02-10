using Domain.Enums;
using Domain.Interfaces;
using Domain.Models;

namespace Domain.Entities;

/// <summary>
/// Пользователи Telegram
/// </summary>
public class TelegramUser : IEntity
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Идентификатор телеграма
    /// </summary>
    public long TelegramId { get; init; }

    /// <summary>
    /// Имя
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Фамилия
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Имя пользователя в телеграм
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Роль пользователя
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Дата присоединения к боту
    /// </summary>
    public DateOnly CreatedAt { get; init; }

    /// <summary>
    /// Добавить пользователя Telegram
    /// </summary>
    /// <param name="userModel">Модель пользователя</param>
    /// <returns>Пользователь телеграм</returns>
    public static TelegramUser Create(AddUserModel userModel)
    {
	    return new TelegramUser
	    {
		    TelegramId = userModel.TelegramId,
		    FirstName = userModel.FirstName,
		    LastName = userModel.LastName,
		    UserName = userModel.UserName,
		    Role = UserRole.User,
		    CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow),
	    };
    }

    public void Update(string? firstName, string? lastName, string? userName)
    {
	    if (!string.IsNullOrWhiteSpace(firstName) && FirstName != firstName)
	    {
		    FirstName = firstName;
	    }

	    if (!string.IsNullOrWhiteSpace(lastName) && LastName != lastName)
	    {
		    LastName = lastName;
	    }

	    if (!string.IsNullOrWhiteSpace(userName) && UserName != userName)
	    {
		    UserName = userName;
	    }
    }
}