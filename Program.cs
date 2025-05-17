using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

public interface IUserService
{
    User CreateUser(string currentUserLogin, bool isCurrentUserAdmin, string login, string password, string name, Gender gender, DateTime? birthday, bool isAdmin);
    User UpdateUserInfo(string currentUserLogin, bool isCurrentUserAdmin, string userLogin, string newName, Gender newGender, DateTime? newBirthday);
    User ChangePassword(string currentUserLogin, bool isCurrentUserAdmin, string userLogin, string newPassword);
    User ChangeLogin(string currentUserLogin, bool isCurrentUserAdmin, string oldLogin, string newLogin);
    List<UserDto> GetAllActiveUsers(bool isCurrentUserAdmin);
    UserDto GetUserByLogin(bool isCurrentUserAdmin, string login);
    UserDto GetUserByLoginAndPassword(string login, string password);
    List<UserDto> GetUsersOlderThan(bool isCurrentUserAdmin, int age);
    bool DeleteUser(string currentUserLogin, bool isCurrentUserAdmin, string userLogin, bool softDelete);
    User RestoreUser(string currentUserLogin, bool isCurrentUserAdmin, string userLogin);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public User CreateUser(string currentUserLogin, bool isCurrentUserAdmin, string login, string password, string name, Gender gender, DateTime? birthday, bool isAdmin)
    {
        if (!isCurrentUserAdmin)
            throw new UnauthorizedAccessException("Only admins can create users");

        ValidateLogin(login);
        ValidatePassword(password);
        ValidateName(name);

        if (_userRepository.UserExists(login))
            throw new ArgumentException("User with this login already exists");

        var newUser = new User
        {
            Login = login,
            Password = password,
            Name = name,
            Gender = gender,
            Birthday = birthday,
            Admin = isAdmin,
            CreatedBy = currentUserLogin,
            ModifiedBy = currentUserLogin
        };

        return _userRepository.AddUser(newUser);
    }

    public User UpdateUserInfo(string currentUserLogin, bool isCurrentUserAdmin, string userLogin, string newName, Gender newGender, DateTime? newBirthday)
    {
        var user = _userRepository.GetUserByLogin(userLogin);
        if (user == null)
            throw new ArgumentException("User not found");

        if (!isCurrentUserAdmin && (currentUserLogin != userLogin || !user.IsActive))
            throw new UnauthorizedAccessException("You don't have permission to update this user");

        if (newName != null)
        {
            ValidateName(newName);
            user.Name = newName;
        }

        user.Gender = newGender;
        user.Birthday = newBirthday;
        user.ModifiedOn = DateTime.UtcNow;
        user.ModifiedBy = currentUserLogin;

        return _userRepository.UpdateUser(user);
    }

    public User ChangePassword(string currentUserLogin, bool isCurrentUserAdmin, string userLogin, string newPassword)
    {
        var user = _userRepository.GetUserByLogin(userLogin);
        if (user == null)
            throw new ArgumentException("User not found");

        if (!isCurrentUserAdmin && (currentUserLogin != userLogin || !user.IsActive))
            throw new UnauthorizedAccessException("You don't have permission to change password");

        ValidatePassword(newPassword);
        user.Password = newPassword;
        user.ModifiedOn = DateTime.UtcNow;
        user.ModifiedBy = currentUserLogin;

        return _userRepository.UpdateUser(user);
    }

    public User ChangeLogin(string currentUserLogin, bool isCurrentUserAdmin, string oldLogin, string newLogin)
    {
        var user = _userRepository.GetUserByLogin(oldLogin);
        if (user == null)
            throw new ArgumentException("User not found");

        if (!isCurrentUserAdmin && (currentUserLogin != oldLogin || !user.IsActive))
            throw new UnauthorizedAccessException("You don't have permission to change login");

        ValidateLogin(newLogin);

        if (_userRepository.UserExists(newLogin))
            throw new ArgumentException("User with this login already exists");

        user.Login = newLogin;
        user.ModifiedOn = DateTime.UtcNow;
        user.ModifiedBy = currentUserLogin;

        return _userRepository.UpdateUser(user);
    }

    public List<UserDto> GetAllActiveUsers(bool isCurrentUserAdmin)
    {
        if (!isCurrentUserAdmin)
            throw new UnauthorizedAccessException("Only admins can get all users");

        return _userRepository.GetAllUsers()
            .Where(u => u.IsActive)
            .OrderBy(u => u.CreatedOn)
            .Select(u => MapToDto(u))
            .ToList();
    }

    public UserDto GetUserByLogin(bool isCurrentUserAdmin, string login)
    {
        if (!isCurrentUserAdmin)
            throw new UnauthorizedAccessException("Only admins can get user by login");

        var user = _userRepository.GetUserByLogin(login);
        if (user == null)
            throw new ArgumentException("User not found");

        return MapToDto(user);
    }

    public UserDto GetUserByLoginAndPassword(string login, string password)
    {
        var user = _userRepository.GetUserByLogin(login);
        if (user == null || user.Password != password)
            throw new ArgumentException("Invalid login or password");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("User is not active");

        return MapToDto(user);
    }

    public List<UserDto> GetUsersOlderThan(bool isCurrentUserAdmin, int age)
    {
        if (!isCurrentUserAdmin)
            throw new UnauthorizedAccessException("Only admins can get users by age");

        return _userRepository.GetAllUsers()
            .Where(u => u.Age > age)
            .Select(u => MapToDto(u))
            .ToList();
    }

    public bool DeleteUser(string currentUserLogin, bool isCurrentUserAdmin, string userLogin, bool softDelete)
    {
        if (!isCurrentUserAdmin)
            throw new UnauthorizedAccessException("Only admins can delete users");

        var user = _userRepository.GetUserByLogin(userLogin);
        if (user == null)
            throw new ArgumentException("User not found");

        if (softDelete)
        {
            user.RevokedOn = DateTime.UtcNow;
            user.RevokedBy = currentUserLogin;
            user.ModifiedOn = DateTime.UtcNow;
            user.ModifiedBy = currentUserLogin;
            _userRepository.UpdateUser(user);
            return true;
        }
        else
        {
            return _userRepository.DeleteUser(user.Id);
        }
    }

    public User RestoreUser(string currentUserLogin, bool isCurrentUserAdmin, string userLogin)
    {
        if (!isCurrentUserAdmin)
            throw new UnauthorizedAccessException("Only admins can restore users");

        var user = _userRepository.GetUserByLogin(userLogin);
        if (user == null)
            throw new ArgumentException("User not found");

        user.RevokedOn = null;
        user.RevokedBy = null;
        user.ModifiedOn = DateTime.UtcNow;
        user.ModifiedBy = currentUserLogin;

        return _userRepository.UpdateUser(user);
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Login = user.Login,
            Name = user.Name,
            Gender = user.Gender,
            Birthday = user.Birthday,
            Admin = user.Admin,
            CreatedOn = user.CreatedOn,
            CreatedBy = user.CreatedBy,
            ModifiedOn = user.ModifiedOn,
            ModifiedBy = user.ModifiedBy,
            IsActive = user.IsActive,
            Age = user.Age
        };
    }

    private void ValidateLogin(string login)
    {
        if (string.IsNullOrWhiteSpace(login) || !Regex.IsMatch(login, @"^[a-zA-Z0-9]+$"))
            throw new ArgumentException("Login can only contain Latin letters and numbers");
    }

    private void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || !Regex.IsMatch(password, @"^[a-zA-Z0-9]+$"))
            throw new ArgumentException("Password can only contain Latin letters and numbers");
    }

    private void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || !Regex.IsMatch(name, @"^[a-zA-Zа-яА-ЯёЁ]+$"))
            throw new ArgumentException("Name can only contain Russian and Latin letters");
    }
}

public interface IUserRepository
{
    User AddUser(User user);
    User UpdateUser(User user);
    bool DeleteUser(Guid userId);
    User GetUserByLogin(string login);
    IEnumerable<User> GetAllUsers();
    bool UserExists(string login);
}