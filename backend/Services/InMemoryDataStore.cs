using System.Collections.Concurrent;
using SupportBot.Models;

namespace SupportBot.Services;

public class InMemoryDataStore
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private readonly ConcurrentDictionary<int, Form> _forms = new();
    private int _nextUserId = 1;
    private int _nextFormId = 1;

    // User operations
    public int GetNextUserId() => Interlocked.Increment(ref _nextUserId);
    
    public bool AddUser(User user)
    {
        return _users.TryAdd(user.Id, user);
    }

    public User? GetUserById(int id)
    {
        _users.TryGetValue(id, out var user);
        return user;
    }

    public User? GetUserByUsername(string username)
    {
        return _users.Values.FirstOrDefault(u => u.Username == username);
    }

    public User? GetUserByEmail(string email)
    {
        return _users.Values.FirstOrDefault(u => u.Email == email);
    }

    public User? GetUserByGithubId(string githubId)
    {
        return _users.Values.FirstOrDefault(u => u.GithubId == githubId);
    }

    public User? GetUserByGoogleId(string googleId)
    {
        return _users.Values.FirstOrDefault(u => u.GoogleId == googleId);
    }

    public IEnumerable<User> GetAllUsers()
    {
        return _users.Values.ToList();
    }

    public bool UpdateUser(User user)
    {
        return _users.TryUpdate(user.Id, user, _users[user.Id]);
    }

    public bool DeleteUser(int id)
    {
        return _users.TryRemove(id, out _);
    }

    // Form operations
    public int GetNextFormId() => Interlocked.Increment(ref _nextFormId);

    public bool AddForm(Form form)
    {
        return _forms.TryAdd(form.Id, form);
    }

    public Form? GetFormById(int id)
    {
        _forms.TryGetValue(id, out var form);
        return form;
    }

    public IEnumerable<Form> GetAllForms()
    {
        return _forms.Values.ToList();
    }

    public IEnumerable<Form> GetFormsByUserId(int userId)
    {
        return _forms.Values.Where(f => f.UserId == userId).ToList();
    }

    public IEnumerable<Form> GetFormsByState(FormState state)
    {
        return _forms.Values.Where(f => f.State == state).ToList();
    }

    public IEnumerable<Form> GetFormsByCategory(FormCategory category)
    {
        return _forms.Values.Where(f => f.Category == category).ToList();
    }

    public IEnumerable<Form> GetFormsByUrgency(FormUrgency urgency)
    {
        return _forms.Values.Where(f => f.Urgency == urgency).ToList();
    }

    public bool UpdateForm(Form form)
    {
        return _forms.TryUpdate(form.Id, form, _forms[form.Id]);
    }

    public bool DeleteForm(int id)
    {
        return _forms.TryRemove(id, out _);
    }

    // Statistics
    public int GetUserCount() => _users.Count;
    public int GetFormCount() => _forms.Count;
}
